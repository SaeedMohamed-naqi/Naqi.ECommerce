// src/Core/Naqi.ECommerce.Domain/Entities/ProductCategory.cs
//
// Represents one entry from the middleware's ui_categories array - the
// categories a product actually appears under on the website, which is
// distinct from Product.CategoryId (the single "primary/mobile" category
// resolved separately in SyncProductsCommandHandler). A product can show
// up under several website categories at once (e.g. both a specific
// device-type category AND a broader parent category), so this is a
// genuine one-to-many collection, not a single FK.
//
// Constructor is internal - entries are only ever created through
// Product.SyncUiCategories(), which owns add/update/remove as one
// operation, same as ProductSpecification/SyncSpecifications.

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class ProductCategory : BaseEntity
{
    public long ProductId { get; private set; }

    // The middleware's own category_id for this ui_categories entry -
    // NOT a FK to our Categories table. Kept separate from
    // Product.CategoryId (the resolved primary Category) since ui_categories
    // entries may not all exist as synced Category rows.
    public long ExternalCategoryId { get; private set; }

    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? Slug { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsLeaf { get; private set; }

    private ProductCategory() { } // EF Core

    internal ProductCategory(
        long productId, long externalCategoryId, string nameEn, string nameAr,
        string? slug, string? imageUrl, bool isPrimary, bool isLeaf)
    {
        ProductId = productId;
        ExternalCategoryId = externalCategoryId;
        NameEn = nameEn;
        NameAr = nameAr;
        Slug = slug;
        ImageUrl = imageUrl;
        IsPrimary = isPrimary;
        IsLeaf = isLeaf;
    }

    internal void UpdateFromSync(string nameEn, string nameAr, string? slug, string? imageUrl, bool isPrimary, bool isLeaf)
    {
        NameEn = nameEn;
        NameAr = nameAr;
        Slug = slug;
        ImageUrl = imageUrl;
        IsPrimary = isPrimary;
        IsLeaf = isLeaf;
    }
}