// src/Core/Naqi.ECommerce.Domain/Entities/Product.cs
//
// Extended to capture the full set of fields NaqiEcommerceMiddleware
// actually returns (title, description, tags/subtags, rating, website
// content blocks, full media list) - not just the minimal name/price/stock
// set from the first pass. Variants, specifications, installations, and
// offers are STILL NOT modeled as separate entities yet - those are
// one-to-many relationships that need their own tables; this pass only
// covers Product's own scalar fields.
//
// Notice: no public setters on business fields. State changes go through
// methods that can enforce invariants and raise domain events - this is
// the "rich domain model" pattern Clean Architecture pushes for, versus
// the anemic "public get; set;" style typical of EF6/CRUD-first projects.

using Naqi.ECommerce.Domain.Common;
using Naqi.ECommerce.Domain.Events;

namespace Naqi.ECommerce.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;

    // Ties this row back to the middleware's product_id for re-sync upserts.
    public long ExternalProductId { get; private set; }

    public decimal Price { get; private set; }
    public decimal? OldPrice { get; private set; }
    public int StockQuantity { get; private set; }

    public long CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    // First image from ProductMedia - used as the list/thumbnail image.
    public string? ImageUrl { get; private set; }

    // ALL images from product_media, stored comma-separated exactly as the
    // middleware sends them - the Details page splits this to render the
    // full gallery. Kept as a single delimited column rather than a
    // separate ProductImage table for now, per the current scope.
    public string? AllImageUrls { get; private set; }

    // ---- Title / description (distinct from Name in the middleware) ----
    public string? TitleEn { get; private set; }
    public string? TitleAr { get; private set; }
    public string? DescriptionEn { get; private set; }
    public string? DescriptionAr { get; private set; }

    // ---- App display: tag/subtag badges ----
    public string? TagEn { get; private set; }
    public string? TagAr { get; private set; }
    public string? SubtagEn { get; private set; }
    public string? SubtagAr { get; private set; }
    public string? TagColor { get; private set; } // hex, e.g. "FF2626"
    public string? SubtagIconUrl { get; private set; }

    public bool IsVertical { get; private set; }
    public decimal RatingAverage { get; private set; }
    public int TotalRating { get; private set; }

    // ---- Website-only long-form content blocks ----
    public string? WebsiteWarranty { get; private set; }
    public string? WebsiteAccessories { get; private set; }
    public string? WebsiteGuidelines { get; private set; }
    public string? WebsiteOtherSpecs { get; private set; } // contains raw HTML from the source system

    public DateTime? LastSyncedAtUtc { get; private set; }

    private readonly List<ProductSpecification> _specifications = new();
    public IReadOnlyCollection<ProductSpecification> Specifications => _specifications.AsReadOnly();

    private readonly List<ProductCategory> _uiCategories = new();
    public IReadOnlyCollection<ProductCategory> UiCategories => _uiCategories.AsReadOnly();

    private Product() { } // EF Core needs a parameterless constructor

    public Product(string nameEn, string nameAr, string sku, decimal price, int stockQuantity, long categoryId)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new ArgumentException("Product name is required.", nameof(nameEn));
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));

        NameEn = nameEn;
        NameAr = nameAr;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
    }

    public void UpdateDetails(string nameEn, string nameAr, decimal price)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new ArgumentException("Product name is required.", nameof(nameEn));
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));

        NameEn = nameEn;
        NameAr = nameAr;
        Price = price;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (quantity > StockQuantity)
            throw new InvalidOperationException($"Insufficient stock for product '{NameEn}'.");

        StockQuantity -= quantity;

        if (StockQuantity == 0)
            AddDomainEvent(new StockDepletedEvent(Id));
    }

    public void Restock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        StockQuantity += quantity;
    }

    // Factory for creating a brand-new local Product from a middleware
    // record during sync (first time this external product is seen).
    public static Product CreateFromSync(ProductSyncData data)
    {
        var product = new Product(data.NameEn, data.NameAr, data.Sku, data.Price, data.StockQuantity, data.CategoryId);
        product.ApplySyncData(data);
        return product;
    }

    // Applies fresh data from the middleware onto an EXISTING local Product
    // (found by ExternalProductId) during re-sync.
    public void UpdateFromSync(ProductSyncData data)
    {
        NameEn = data.NameEn;
        NameAr = data.NameAr;
        ApplySyncData(data);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    private void ApplySyncData(ProductSyncData data)
    {
        ExternalProductId = data.ExternalProductId;
        Price = data.Price;
        OldPrice = data.OldPrice;
        StockQuantity = data.StockQuantity;
        CategoryId = data.CategoryId;
        ImageUrl = data.ImageUrl;
        AllImageUrls = data.AllImageUrls;
        TitleEn = data.TitleEn;
        TitleAr = data.TitleAr;
        DescriptionEn = data.DescriptionEn;
        DescriptionAr = data.DescriptionAr;
        TagEn = data.TagEn;
        TagAr = data.TagAr;
        SubtagEn = data.SubtagEn;
        SubtagAr = data.SubtagAr;
        TagColor = data.TagColor;
        SubtagIconUrl = data.SubtagIconUrl;
        IsVertical = data.IsVertical;
        RatingAverage = data.RatingAverage;
        TotalRating = data.TotalRating;
        WebsiteWarranty = data.WebsiteWarranty;
        WebsiteAccessories = data.WebsiteAccessories;
        WebsiteGuidelines = data.WebsiteGuidelines;
        WebsiteOtherSpecs = data.WebsiteOtherSpecs;
        LastSyncedAtUtc = DateTime.UtcNow;
    }

    // Upserts + prunes the specification collection as one operation, so
    // the Product aggregate is always left in a consistent state: any
    // spec no longer present in `incoming` is removed (the middleware is
    // the source of truth), matching ones are updated, new ones are added.
    // This logic lives here (not in the sync command handler) because
    // "what does it mean to sync a product's specs" is a rule about the
    // Product aggregate, not an application-layer orchestration concern.
    public void SyncSpecifications(IEnumerable<ProductSpecificationSyncData> incoming)
    {
        var incomingList = incoming.ToList();
        var incomingIds = incomingList.Select(s => s.ExternalSpecificationId).ToHashSet();

        _specifications.RemoveAll(s => !incomingIds.Contains(s.ExternalSpecificationId));

        foreach (var specData in incomingList)
        {
            var existing = _specifications.FirstOrDefault(s => s.ExternalSpecificationId == specData.ExternalSpecificationId);

            if (existing is not null)
            {
                existing.UpdateFromSync(specData.TitleEn, specData.TitleAr, specData.ValueEn, specData.ValueAr);
            }
            else
            {
                // Id may still be 0 here for a brand-new Product not yet
                // saved - that's fine, EF Core's relationship fixup
                // corrects ProductId automatically at SaveChanges time
                // based on the navigation collection, regardless of what's
                // set here in memory.
                _specifications.Add(new ProductSpecification(
                    Id, specData.ExternalSpecificationId,
                    specData.TitleEn, specData.TitleAr, specData.ValueEn, specData.ValueAr));
            }
        }
    }

    // Same upsert-and-prune pattern as SyncSpecifications, for the
    // middleware's ui_categories array.
    public void SyncUiCategories(IEnumerable<ProductCategorySyncData> incoming)
    {
        var incomingList = incoming.ToList();
        var incomingIds = incomingList.Select(c => c.ExternalCategoryId).ToHashSet();

        _uiCategories.RemoveAll(c => !incomingIds.Contains(c.ExternalCategoryId));

        foreach (var catData in incomingList)
        {
            var existing = _uiCategories.FirstOrDefault(c => c.ExternalCategoryId == catData.ExternalCategoryId);

            if (existing is not null)
            {
                existing.UpdateFromSync(catData.NameEn, catData.NameAr, catData.Slug, catData.ImageUrl, catData.IsPrimary, catData.IsLeaf);
            }
            else
            {
                _uiCategories.Add(new ProductCategory(
                    Id, catData.ExternalCategoryId, catData.NameEn, catData.NameAr,
                    catData.Slug, catData.ImageUrl, catData.IsPrimary, catData.IsLeaf));
            }
        }
    }
}

// Groups everything CreateFromSync/UpdateFromSync need - avoids an
// unwieldy 20-parameter method signature. Lives alongside Product since
// it's purely a shape for constructing/updating one.
public record ProductSyncData(
    string NameEn,
    string NameAr,
    string Sku,
    long ExternalProductId,
    decimal Price,
    decimal? OldPrice,
    int StockQuantity,
    long CategoryId,
    string? ImageUrl,
    string? AllImageUrls,
    string? TitleEn,
    string? TitleAr,
    string? DescriptionEn,
    string? DescriptionAr,
    string? TagEn,
    string? TagAr,
    string? SubtagEn,
    string? SubtagAr,
    string? TagColor,
    string? SubtagIconUrl,
    bool IsVertical,
    decimal RatingAverage,
    int TotalRating,
    string? WebsiteWarranty,
    string? WebsiteAccessories,
    string? WebsiteGuidelines,
    string? WebsiteOtherSpecs);

public record ProductSpecificationSyncData(
    long ExternalSpecificationId,
    string TitleEn,
    string TitleAr,
    string ValueEn,
    string ValueAr);

public record ProductCategorySyncData(
    long ExternalCategoryId,
    string NameEn,
    string NameAr,
    string? Slug,
    string? ImageUrl,
    bool IsPrimary,
    bool IsLeaf);