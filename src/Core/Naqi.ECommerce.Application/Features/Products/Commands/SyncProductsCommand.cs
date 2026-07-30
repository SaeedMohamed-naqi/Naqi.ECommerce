// src/Core/Naqi.ECommerce.Application/Features/Products/Commands/SyncProductsCommand.cs
//
// PERFORMANCE: rewritten to batch-load everything a page needs in a
// handful of queries instead of one round trip PER PRODUCT. The previous
// version called _context.Products.FirstOrDefaultAsync(...) once per
// product (100 round trips per page), and ResolveCategoryAsync/
// ResolveOfferGroupAsync each did their own FirstOrDefaultAsync PLUS an
// immediate SaveChangesAsync() for every newly-created Category/OfferGroup
// (another 1-2 round trips per NEW category/offer group encountered,
// potentially hundreds more). For a 100-product page this could easily
// mean 300-500+ round trips.
//
// Now, for each page:
//   1. ONE query loads every existing Product this page references
//      (by ExternalProductId IN (...)), with all child collections included.
//   2. ONE query loads every existing Category this page's primary
//      ui_categories entries need; any missing ones are created in memory.
//   3. ONE query loads every existing OfferGroup this page's offers need;
//      any missing ones are created in memory.
//   4. ONE SaveChangesAsync assigns real Ids to anything newly created
//      in steps 2-3, so the per-product loop below can resolve FKs from
//      in-memory dictionaries with zero further DB calls.
//   5. The per-product loop runs entirely in memory (no queries, no saves).
//   6. ONE final SaveChangesAsync persists every product + all their
//      child collections for the whole page.
//
// Net result: ~5-6 round trips per page instead of hundreds.

using MediatR;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware;
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
    private const string UncategorizedSlug = "uncategorized";

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
        var totalCreated = 0;
        var totalUpdated = 0;
        var allSeenExternalIds = new HashSet<long>();

        try
        {
            var hasMore = true;

            while (hasMore)
            {
                var page = await _middlewareClient.GetProductsPageAsync(skip, PageSize, cancellationToken);

                if (!page.Status || page.Data.Count == 0)
                    break;

                var (created, updated) = await ProcessPageAsync(page.Data, cancellationToken);
                totalCreated += created;
                totalUpdated += updated;
                totalFetched += page.Data.Count;

                foreach (var item in page.Data)
                    allSeenExternalIds.Add(item.ProductId);

                hasMore = page.Data.Count == PageSize;
                skip += page.Data.Count;
            }

            // A full sync run just walked every page the middleware has -
            // anything still active in our DB but never seen this run no
            // longer exists at the source, so soft-delete it (not a hard
            // delete - Orders may still reference it historically).
            //
            // Safety guard: if we never saw ANY product this run
            // (allSeenExternalIds is empty), skip this entirely rather than
            // soft-deleting the whole catalog - an empty/failed first page
            // is ambiguous between "genuinely no products" and a transient
            // API glitch, and wiping every product because of a hiccup is
            // far too risky to do automatically.
            if (allSeenExternalIds.Count > 0)
            {
                var disappeared = await _context.Products
                    .Where(p => !allSeenExternalIds.Contains(p.ExternalProductId))
                    .ToListAsync(cancellationToken);

                foreach (var product in disappeared)
                {
                    product.SoftDelete(Naqi.ECommerce.Domain.Common.ChildCollectionSyncer.SyncNotPresentReason);
                }

                if (disappeared.Count > 0)
                    await _context.SaveChangesAsync(cancellationToken);
            }

            return new SyncProductsResult(totalFetched, totalCreated, totalUpdated, Success: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Product sync failed after fetching {Skip} records ({Created} created, {Updated} updated so far).",
                skip, totalCreated, totalUpdated);
            return new SyncProductsResult(totalFetched, totalCreated, totalUpdated, Success: false, ErrorMessage: ex.Message);
        }
    }

    private async Task<(int created, int updated)> ProcessPageAsync(
        List<MiddlewareProduct> items, CancellationToken cancellationToken)
    {
        // ---- 1. Bulk-load existing products for this whole page ----
        var externalProductIds = items.Select(i => i.ProductId).ToList();

        // IgnoreQueryFilters() here is intentional - a product that
        // disappeared in a prior sync (soft-deleted) needs to be findable
        // again so it can be RESTORED if it reappears, instead of the
        // default filter hiding it and causing a duplicate insert.
        //
        // AsSplitQuery() is important here: five .Include()s in one query
        // would otherwise generate a single SQL statement with five JOINs,
        // and the row count multiplies across every combination (a product
        // with 10 specs x 3 variants x 2 offers returns 60 duplicated rows
        // of the product's own columns, repeated per combination) - for
        // 100 products with realistic child counts that's enormous
        // over-fetching. AsSplitQuery() issues one query per collection
        // instead, so each row is only fetched once.
        var existingProducts = (await _context.Products
                .IgnoreQueryFilters()
                .Include(p => p.Specifications)
                .Include(p => p.UiCategories)
                .Include(p => p.Installations)
                .Include(p => p.Variants)
                .Include(p => p.Offers)
                
                 .Where(p => externalProductIds.Contains(p.ExternalProductId))
                .ToListAsync(cancellationToken))
            .ToDictionary(p => p.ExternalProductId);

        // ---- 2. Bulk-resolve primary categories needed this page ----
        var categoriesByExternalId = await BulkResolveCategoriesAsync(items, cancellationToken);
        var uncategorized = await GetOrCreateUncategorizedAsync(items, cancellationToken);

        // ---- 3. Bulk-resolve offer groups needed this page ----
        var offerGroupsByExternalId = await BulkResolveOfferGroupsAsync(items, cancellationToken);

        // ---- 4. Flush new Categories/OfferGroups so they get real Ids ----
        // before the per-product loop needs to reference them as FKs.
        await _context.SaveChangesAsync(cancellationToken);

        // ---- 5. Process every product in memory - no DB calls in this loop ----
        var created = 0;
        var updated = 0;

        foreach (var item in items)
        {
            var primaryUiCategory = item.UiCategories?.FirstOrDefault(c => c.IsPrimary)
                ?? item.UiCategories?.FirstOrDefault();

            var categoryId = primaryUiCategory is not null
                ? categoriesByExternalId[primaryUiCategory.CategoryId].Id
                : uncategorized.Id;

            var allImages = item.ProductMedia?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(url => url.Trim())
                .Where(url => url.Length > 0)
                .ToList();

            var imageUrl = allImages?.FirstOrDefault();
            var allImageUrls = allImages is { Count: > 0 } ? string.Join(",", allImages) : null;

            var quantity = item.ProductQuantity?.Qnty
                ?? item.Variants?.FirstOrDefault()?.ProductQnty
                ?? item.Quantity;

            // Mapster handles every plain-rename/same-name field (see
            // MappingConfig's MiddlewareProduct -> ProductSyncData
            // registration) - only the genuinely COMPUTED fields (things
            // that need actual logic, not a field copy) are patched in
            // via `with` afterward.
            var syncData = item.Adapt<ProductSyncData>() with
            {
                Sku = item.ProductId.ToString(), // middleware doesn't expose a separate SKU - product_id doubles as one
                OldPrice = item.ProductPrice != item.OnSalePrice ? item.ProductPrice : null,
                StockQuantity = quantity,
                CategoryId = categoryId,
                ImageUrl = imageUrl,
                AllImageUrls = allImageUrls,
                IsVertical = item.IsVertical == 1
            };

            Product product;
            if (existingProducts.TryGetValue(item.ProductId, out var existing))
            {
                if (existing.IsDeleted)
                    existing.Restore(); // reappeared after being gone in a previous sync

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

            var specData = (item.Specifications ?? new List<MiddlewareSpecification>())
                .Select(s => new ProductSpecificationSyncData(
                    s.Id, s.TitleEn, s.TitleAr, s.DescriptionNameEn, s.DescriptionNameAr));
            product.SyncSpecifications(specData);

            var uiCategoryData = (item.UiCategories ?? new List<MiddlewareUiCategory>())
                .Select(c => new ProductCategorySyncData(
                    c.CategoryId, c.NameEn, c.NameAr, c.Slug, c.Image, c.IsPrimary, c.IsLeaf));
            product.SyncUiCategories(uiCategoryData);

            var installationData = (item.ProductInstallations ?? new List<MiddlewareProductInstallation>())
                .Select(i => new ProductInstallationSyncData(
                    i.InstallationId, i.TitleEn, i.TitleAr, i.Price, i.IsSelected == 1));
            product.SyncInstallations(installationData);

            var variantData = (item.Variants ?? new List<MiddlewareVariant>())
                .Select(v => new ProductVariantSyncData(
                    v.Id, v.ProductNameEn, v.ProductNameAr, v.ColorEn, v.ColorAr, v.ColorCode,
                    v.OnSalePrice, v.ProductPrice != v.OnSalePrice ? v.ProductPrice : null,
                    v.ProductQnty, v.ProductMedia));
            product.SyncVariants(variantData);

            var offerData = (item.Offers ?? new List<MiddlewareOffer>())
                .Select(o => new ProductOfferSyncData(
                    o.OfferId, offerGroupsByExternalId[o.OfferGroupId].Id, o.Status == 1));
            product.SyncOffers(offerData);
        }

        // ---- 6. One final save for every product + child collection this page ----
        await _context.SaveChangesAsync(cancellationToken);

        return (created, updated);
    }

    // Bulk find-or-create for every DISTINCT primary category this page's
    // products reference - one query, one batch of in-memory creates,
    // instead of a query (and a save!) per product.
    private async Task<Dictionary<long, Category>> BulkResolveCategoriesAsync(
        List<MiddlewareProduct> items, CancellationToken cancellationToken)
    {
        var neededCategories = items
            .Select(i => i.UiCategories?.FirstOrDefault(c => c.IsPrimary) ?? i.UiCategories?.FirstOrDefault())
            .Where(c => c is not null)
            .GroupBy(c => c!.CategoryId)
            .Select(g => g.First()!)
            .ToList();

        if (neededCategories.Count == 0)
            return new Dictionary<long, Category>();

        var neededIds = neededCategories.Select(c => c.CategoryId).ToList();

        var existing = (await _context.Categories
                .IgnoreQueryFilters() // find-and-restore previously soft-deleted categories instead of duplicating
                .Where(c => c.ExternalCategoryId != null && neededIds.Contains(c.ExternalCategoryId!.Value))
                .ToListAsync(cancellationToken))
            .ToDictionary(c => c.ExternalCategoryId!.Value);

        foreach (var categoryData in neededCategories)
        {
            if (existing.TryGetValue(categoryData.CategoryId, out var existingCategory))
            {
                if (existingCategory.IsDeleted)
                    existingCategory.Restore();

                existingCategory.UpdateFromSync(categoryData.NameEn, categoryData.NameAr, categoryData.Image);
            }
            else
            {
                var newCategory = new Category(categoryData.NameEn, categoryData.NameAr, categoryData.CategoryId, categoryData.Image);
                _context.Categories.Add(newCategory);
                existing[categoryData.CategoryId] = newCategory;
            }
        }

        return existing;
    }

    // Fallback "Uncategorized" bucket for products with no ui_categories at
    // all - shared across the whole page (and across pages/runs), found by
    // Slug since it has no real ExternalCategoryId.
    private async Task<Category> GetOrCreateUncategorizedAsync(
        List<MiddlewareProduct> items, CancellationToken cancellationToken)
    {
        var needsUncategorized = items.Any(i => i.UiCategories is null || i.UiCategories.Count == 0);
        if (!needsUncategorized)
        {
            // Won't be used this page, but callers still index into it for
            // products with no ui_categories - if that never happens this
            // page, returning a throwaway instance that's never touched is fine.
            return new Category("Uncategorized", "غير مصنف", externalCategoryId: null, imageUrl: null, slug: UncategorizedSlug);
        }

        var uncategorized = await _context.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Slug == UncategorizedSlug, cancellationToken);

        if (uncategorized is not null)
        {
            if (uncategorized.IsDeleted)
                uncategorized.Restore();

            return uncategorized;
        }

        var newUncategorized = new Category("Uncategorized", "غير مصنف", externalCategoryId: null, imageUrl: null, slug: UncategorizedSlug);
        _context.Categories.Add(newUncategorized);
        return newUncategorized;
    }

    // Bulk find-or-create for every DISTINCT offer group this page's
    // products reference - one query instead of one-per-offer, and no
    // per-new-group SaveChangesAsync call.
    private async Task<Dictionary<long, OfferGroup>> BulkResolveOfferGroupsAsync(
        List<MiddlewareProduct> items, CancellationToken cancellationToken)
    {
        var neededOffers = items
            .SelectMany(i => i.Offers ?? new List<MiddlewareOffer>())
            .GroupBy(o => o.OfferGroupId)
            .Select(g => g.First())
            .ToList();

        if (neededOffers.Count == 0)
            return new Dictionary<long, OfferGroup>();

        var neededIds = neededOffers.Select(o => o.OfferGroupId).ToList();

        var existing = (await _context.OfferGroups
                .IgnoreQueryFilters() // find-and-restore previously soft-deleted offer groups instead of duplicating
                .Where(g => neededIds.Contains(g.ExternalOfferGroupId))
                .ToListAsync(cancellationToken))
            .ToDictionary(g => g.ExternalOfferGroupId);

        foreach (var offer in neededOffers)
        {
            var group = offer.OfferGroup;
            var nameEn = group?.OfferNameEn ?? $"Offer Group {offer.OfferGroupId}";
            var nameAr = group?.OfferNameAr ?? nameEn;
            var iconUrl = group?.OfferIcon;
            var color = group?.OfferColor;
            var isBig = group?.IsBig ?? false;
            var expireAtUtc = group?.ExpireAt?.UtcDateTime;

            if (existing.TryGetValue(offer.OfferGroupId, out var existingGroup))
            {
                if (existingGroup.IsDeleted)
                    existingGroup.Restore();

                existingGroup.UpdateFromSync(nameEn, nameAr, iconUrl, color, isBig, expireAtUtc);
            }
            else
            {
                var newGroup = new OfferGroup(offer.OfferGroupId, nameEn, nameAr, iconUrl, color, isBig, expireAtUtc);
                _context.OfferGroups.Add(newGroup);
                existing[offer.OfferGroupId] = newGroup;
            }
        }

        return existing;
    }
}