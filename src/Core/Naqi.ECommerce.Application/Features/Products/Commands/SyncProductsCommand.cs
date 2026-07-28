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
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware;
using Naqi.ECommerce.Domain.Entities;

namespace Naqi.ECommerce.Application.Features.Products.Commands;

public record SyncProductsCommand : IRequest<SyncProductsResult>;

public record SyncProductsResult(int TotalFetched, int Created, int Updated, bool Success, string? ErrorMessage = null);
public record SyncProductsSummaryDto(int TotalFetched, int Created, int Updated);
public class SyncProductsCommandHandler : IRequestHandler<SyncProductsCommand, SyncProductsResult>
{
    private const int PageSize = 100;

    private readonly INaqiMiddlewareClient _middlewareClient;
    private readonly IApplicationDbContext _context;

    public SyncProductsCommandHandler(INaqiMiddlewareClient middlewareClient, IApplicationDbContext context)
    {
        _middlewareClient = middlewareClient;
        _context = context;
    }

    public async Task<SyncProductsResult> Handle(SyncProductsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var skip = 0;
            var hasMore = true;
            var totalFetched = 0;
            var created = 0;
            var updated = 0;

            while (hasMore)
            {
                var page = await _middlewareClient.GetProductsPageAsync(skip, PageSize, cancellationToken);

                if (!page.Status || page.Data.Count == 0)
                    break;

                foreach (var item in page.Data)
                {
                    var categoryId = await ResolveCategoryAsync(item, cancellationToken);

                    var imageUrl = item.ProductMedia?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault()?.Trim();

                    var quantity = item.ProductQuantity?.Qnty
                        ?? item.Variants?.FirstOrDefault()?.ProductQnty
                        ?? item.Quantity;

                    var existing = await _context.Products
                        .FirstOrDefaultAsync(p => p.ExternalProductId == item.ProductId, cancellationToken);

                    if (existing is not null)
                    {
                        existing.UpdateFromSync(
                            item.ProductNameEn, item.ProductNameAr,
                            item.onsaleprice, item.ProductPrice,
                            quantity, categoryId, imageUrl);
                        updated++;
                    }
                    else
                    {
                        var product = Product.CreateFromSync(
                            externalProductId: item.ProductId,
                            nameEn: item.ProductNameEn,
                            nameAr: item.ProductNameAr,
                            //sku: item.ProductId, // middleware doesn't expose a separate SKU - product_id doubles as one
                            price: item.onsaleprice,
                            oldPrice: item.ProductPrice != item.onsaleprice ? item.ProductPrice : null,
                            stockQuantity: quantity,
                            categoryId: categoryId,
                            imageUrl: imageUrl);

                        _context.Products.Add(product);
                        created++;
                    }
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
            return new SyncProductsResult(0, 0, 0, Success: false, ErrorMessage: ex.Message);
        }
    }

    private async Task<long> ResolveCategoryAsync(    MiddlewareProduct item,
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
}