// src/Core/Naqi.ECommerce.Application/Features/Products/Commands/SyncProductsCommand.cs
//
// Ports the pagination loop from the legacy GetProducts() action:
// keep fetching {skip, count} pages from the middleware until a page
// comes back smaller than the requested count, upserting each product
// (and its primary category) into the local database as it goes.
//
// Upsert key: Product.ExternalProductId (== middleware's product_id).
// Category upsert key: Category.ExternalCategoryId (== ui_categories
// primary entry's category_id).

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Domain.Entities;

namespace Naqi.ECommerce.Application.Features.Products.Commands;

public record SyncProductsCommand : IRequest<SyncProductsResult>;

public record SyncProductsResult(int TotalFetched, int Created, int Updated, bool Success, string? ErrorMessage = null);

// Trimmed payload actually sent to the client - SyncProductsResult carries
// Success/ErrorMessage for the handler's own use, but those get folded into
// ApiResponse's Success/Message at the controller level instead of being
// duplicated in the JSON body.
public record SyncProductsSummaryDto(int TotalFetched, int Created, int Updated);

public class SyncProductsCommandHandler : IRequestHandler<SyncProductsCommand, SyncProductsResult>
{
    private const int PageSize = 100;

    private readonly INaqiMiddlewareClient _middlewareClient;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SyncProductsCommandHandler> _logger;

    public SyncProductsCommandHandler(
        INaqiMiddlewareClient middlewareClient, IApplicationDbContext context, ILogger<SyncProductsCommandHandler> logger)
    {
        _middlewareClient = middlewareClient;
        _context = context;
        _logger = logger;
    }

    public async Task<SyncProductsResult> Handle(SyncProductsCommand request, CancellationToken cancellationToken)
    {
        var skip = 0;
        var totalFetched = 0;
        var created = 0;
        var updated = 0;

        try
        {
            var hasMore = true;

            while (hasMore)
            {
                var page = await _middlewareClient.GetProductsPageAsync(skip, PageSize, cancellationToken);

                if (!page.Status || page.Data.Count == 0)
                    break;

                foreach (var item in page.Data)
                {
                    var categoryId = await ResolveCategoryAsync(item, cancellationToken);

                    var allImages = item.ProductMedia?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(url => url.Trim())
                        .Where(url => url.Length > 0)
                        .ToList();

                    var imageUrl = allImages?.FirstOrDefault();
                    var allImageUrls = allImages is { Count: > 0 } ? string.Join(",", allImages) : null;

                    var quantity = item.ProductQuantity?.Qnty
                        ?? item.Variants?.FirstOrDefault()?.ProductQnty
                        ?? item.Quantity;

                    var syncData = new ProductSyncData(
                        NameEn: item.ProductNameEn,
                        NameAr: item.ProductNameAr,
                        Sku: item.ProductId.ToString(), // middleware doesn't expose a separate SKU - product_id doubles as one
                        ExternalProductId: item.ProductId,
                        Price: item.OnSalePrice,
                        OldPrice: item.ProductPrice != item.OnSalePrice ? item.ProductPrice : null,
                        StockQuantity: quantity,
                        CategoryId: categoryId,
                        ImageUrl: imageUrl,
                        AllImageUrls: allImageUrls,
                        TitleEn: item.ProductTitleEn,
                        TitleAr: item.ProductTitleAr,
                        DescriptionEn: item.ProductDescriptionEn,
                        DescriptionAr: item.ProductDescriptionAr,
                        TagEn: item.Tag,
                        TagAr: item.TagAr,
                        SubtagEn: item.Subtag,
                        SubtagAr: item.SubtagAr,
                        TagColor: item.TagColor,
                        SubtagIconUrl: item.Subtagicon,
                        IsVertical: item.IsVertical == 1,
                        RatingAverage: item.RatingAverage,
                        TotalRating: item.TotalRating,
                        WebsiteWarranty: item.WebsiteWarranty,
                        WebsiteAccessories: item.WebsiteAccessories,
                        WebsiteGuidelines: item.WebsiteGuidelines,
                        WebsiteOtherSpecs: item.WebsiteOtherSpecs);

                    var existing = await _context.Products
                        .Include(p => p.Specifications)
                        .Include(p => p.UiCategories)
                        .Include(p => p.Installations)
                        .Include(p => p.Variants)
                        .Include(p => p.Offers)
                        .FirstOrDefaultAsync(p => p.ExternalProductId == item.ProductId, cancellationToken);

                    Product product;
                    if (existing is not null)
                    {
                        existing.UpdateFromSync(syncData);
                        product = existing;
                        updated++;
                    }
                    else
                    {
                        product = Product.CreateFromSync(syncData);
                        _context.Products.Add(product);
                        created++;
                    }

                    var specData = (item.Specifications ?? new List<Application.Common.Interfaces.NaqiMiddleware.MiddlewareSpecification>())
                        .Select(s => new ProductSpecificationSyncData(
                            s.Id, s.TitleEn, s.TitleAr, s.DescriptionNameEn, s.DescriptionNameAr));

                    product.SyncSpecifications(specData);

                    var uiCategoryData = (item.UiCategories ?? new List<Application.Common.Interfaces.NaqiMiddleware.MiddlewareUiCategory>())
                        .Select(c => new ProductCategorySyncData(
                            c.CategoryId, c.NameEn, c.NameAr, c.Slug, c.Image, c.IsPrimary, c.IsLeaf));

                    product.SyncUiCategories(uiCategoryData);

                    var installationData = (item.ProductInstallations ?? new List<Application.Common.Interfaces.NaqiMiddleware.MiddlewareProductInstallation>())
                        .Select(i => new ProductInstallationSyncData(
                            i.InstallationId, i.TitleEn, i.TitleAr, i.Price, i.IsSelected == 1));

                    product.SyncInstallations(installationData);

                    var variantData = (item.Variants ?? new List<Application.Common.Interfaces.NaqiMiddleware.MiddlewareVariant>())
                        .Select(v => new ProductVariantSyncData(
                            v.Id, v.ProductNameEn, v.ProductNameAr, v.ColorEn, v.ColorAr, v.ColorCode,
                            v.OnSalePrice, v.ProductPrice != v.OnSalePrice ? v.ProductPrice : null,
                            v.ProductQnty, v.ProductMedia));

                    product.SyncVariants(variantData);

                    var offerData = new List<ProductOfferSyncData>();
                    foreach (var offer in item.Offers ?? new List<Application.Common.Interfaces.NaqiMiddleware.MiddlewareOffer>())
                    {
                        var offerGroupId = await ResolveOfferGroupAsync(offer, cancellationToken);
                        offerData.Add(new ProductOfferSyncData(offer.OfferId, offerGroupId, offer.Status == 1));
                    }

                    product.SyncOffers(offerData);
                }

                totalFetched += page.Data.Count;
                await _context.SaveChangesAsync(cancellationToken);

                hasMore = page.Data.Count == PageSize;
                skip += page.Data.Count;
            }

            return new SyncProductsResult(totalFetched, created, updated, Success: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Product sync failed after fetching {Skip} records ({Created} created, {Updated} updated so far).",
                skip, created, updated);
            return new SyncProductsResult(totalFetched, created, updated, Success: false, ErrorMessage: ex.Message);
        }
    }

    private async Task<long> ResolveCategoryAsync(
        Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware.MiddlewareProduct item,
        CancellationToken cancellationToken)
    {
        var primaryCategory = item.UiCategories?.FirstOrDefault(c => c.IsPrimary)
            ?? item.UiCategories?.FirstOrDefault();

        if (primaryCategory is null)
        {
            // Fallback "Uncategorized" bucket - keeps sync from failing
            // outright if a product has no category info in this payload.
            var uncategorized = await _context.Categories
                .FirstOrDefaultAsync(c => c.ExternalCategoryId == "uncategorized", cancellationToken);

            if (uncategorized is not null)
                return uncategorized.Id;

            var newUncategorized = new Category("Uncategorized", "غير مصنف", "uncategorized");
            _context.Categories.Add(newUncategorized);
            await _context.SaveChangesAsync(cancellationToken);
            return newUncategorized.Id;
        }

        var externalCategoryId = primaryCategory.CategoryId.ToString();
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.ExternalCategoryId == externalCategoryId, cancellationToken);

        if (existingCategory is not null)
        {
            existingCategory.UpdateFromSync(primaryCategory.NameEn, primaryCategory.NameAr, primaryCategory.Image);
            return existingCategory.Id;
        }

        var newCategory = new Category(primaryCategory.NameEn, primaryCategory.NameAr, externalCategoryId, primaryCategory.Image);
        _context.Categories.Add(newCategory);
        await _context.SaveChangesAsync(cancellationToken);
        return newCategory.Id;
    }

    // Resolves (or creates) the shared OfferGroup for one offer entry -
    // same find-or-create pattern as ResolveCategoryAsync, since offer
    // groups are shared campaigns rather than per-product data.
    private async Task<long> ResolveOfferGroupAsync(
        Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware.MiddlewareOffer offer,
        CancellationToken cancellationToken)
    {
        var group = offer.OfferGroup;

        var existingGroup = await _context.OfferGroups
            .FirstOrDefaultAsync(g => g.ExternalOfferGroupId == offer.OfferGroupId, cancellationToken);

        var nameEn = group?.OfferNameEn ?? $"Offer Group {offer.OfferGroupId}";
        var nameAr = group?.OfferNameAr ?? nameEn;
        var iconUrl = group?.OfferIcon;
        var color = group?.OfferColor;
        var isBig = group?.IsBig ?? false;
        var expireAtUtc = group?.ExpireAt?.UtcDateTime;

        if (existingGroup is not null)
        {
            existingGroup.UpdateFromSync(nameEn, nameAr, iconUrl, color, isBig, expireAtUtc);
            return existingGroup.Id;
        }

        var newGroup = new OfferGroup(offer.OfferGroupId, nameEn, nameAr, iconUrl, color, isBig, expireAtUtc);
        _context.OfferGroups.Add(newGroup);
        await _context.SaveChangesAsync(cancellationToken);
        return newGroup.Id;
    }
}