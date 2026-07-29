// src/Core/Naqi.ECommerce.Domain/Entities/Category.cs
//
// Expanded to match /api/v2/categories/tree's full response shape -
// hierarchy (ParentId, self-referencing), SEO fields, visibility/feature
// flags, and a Banners child collection. ExternalCategoryId is nullable:
// it's null ONLY for the internal "Uncategorized" fallback bucket created
// by SyncProductsCommandHandler.ResolveCategoryAsync when a product
// references a category_id the tree sync hasn't (or never will) return -
// that fallback is found again via Slug ("uncategorized"), not an external
// id, since it doesn't have one.

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class Category : BaseAuditableEntity
{
    // Null only for the internal "Uncategorized" fallback bucket.
    public long? ExternalCategoryId { get; private set; }

    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? Slug { get; private set; }
    public string? DescriptionEn { get; private set; }
    public string? DescriptionAr { get; private set; }

    public long? ParentId { get; private set; }
    public Category? Parent { get; private set; }

    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }
    public string? VisibilityChannel { get; private set; } // "website" / "app" / "both"
    public bool IsFeatured { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public bool DisplayDescendantProducts { get; private set; }
    public bool ShowChildCategories { get; private set; }

    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? MetaKeywords { get; private set; }
    public string? CanonicalUrl { get; private set; }

    private readonly List<CategoryBanner> _banners = new();
    public IReadOnlyCollection<CategoryBanner> Banners => _banners.AsReadOnly();

    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private Category() { }

    // Minimal constructor - used ONLY by SyncProductsCommandHandler's
    // ResolveCategoryAsync fallback, when a product references a category
    // the dedicated tree sync hasn't populated yet (or the "Uncategorized"
    // bucket). Sensible defaults keep it visible/functional until the
    // real tree sync (if it ever covers this id) enriches it properly.
    public Category(string nameEn, string nameAr, long? externalCategoryId = null, string? imageUrl = null, string? slug = null)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new ArgumentException("Category name (EN) is required.", nameof(nameEn));

        NameEn = nameEn;
        NameAr = nameAr;
        ExternalCategoryId = externalCategoryId;
        ImageUrl = imageUrl;
        Slug = slug;
        IsActive = true;
        DisplayDescendantProducts = true;
        ShowChildCategories = true;
    }

    public void UpdateFromSync(string nameEn, string nameAr, string? imageUrl)
    {
        NameEn = nameEn;
        NameAr = nameAr;
        ImageUrl = imageUrl;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    // Factory used by the dedicated category tree sync
    // (SyncCategoriesCommandHandler) - carries the FULL set of fields the
    // tree endpoint returns, unlike the minimal constructor above.
    public static Category CreateFromSync(CategorySyncData data)
    {
        var category = new Category(data.NameEn, data.NameAr, data.ExternalCategoryId, data.ImageUrl, data.Slug);
        category.ApplyTreeSyncData(data);
        return category;
    }

    public void UpdateFromTreeSync(CategorySyncData data)
    {
        NameEn = data.NameEn;
        NameAr = data.NameAr;
        ApplyTreeSyncData(data);
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    private void ApplyTreeSyncData(CategorySyncData data)
    {
        Slug = data.Slug;
        DescriptionEn = data.DescriptionEn;
        DescriptionAr = data.DescriptionAr;
        ParentId = data.ParentId;
        ImageUrl = data.ImageUrl;
        IsActive = data.IsActive;
        VisibilityChannel = data.VisibilityChannel;
        IsFeatured = data.IsFeatured;
        DisplayOrder = data.DisplayOrder;
        EndsAtUtc = data.EndsAtUtc;
        DisplayDescendantProducts = data.DisplayDescendantProducts;
        ShowChildCategories = data.ShowChildCategories;
        MetaTitle = data.MetaTitle;
        MetaDescription = data.MetaDescription;
        MetaKeywords = data.MetaKeywords;
        CanonicalUrl = data.CanonicalUrl;
    }

    // Same upsert-and-prune pattern used throughout Product's child
    // collections (Specifications, Variants, etc.).
    public void SyncBanners(IEnumerable<CategoryBannerSyncData> incoming)
    {
        var incomingList = incoming.ToList();
        var incomingIds = incomingList.Select(b => b.ExternalBannerId).ToHashSet();

        _banners.RemoveAll(b => !incomingIds.Contains(b.ExternalBannerId));

        foreach (var bannerData in incomingList)
        {
            var existing = _banners.FirstOrDefault(b => b.ExternalBannerId == bannerData.ExternalBannerId);

            if (existing is not null)
            {
                existing.UpdateFromSync(
                    bannerData.ImageUrl, bannerData.MobileImageUrl, bannerData.Title, bannerData.Subtitle,
                    bannerData.Description, bannerData.ButtonText, bannerData.ButtonUrl,
                    bannerData.DisplayOrder, bannerData.StartsAtUtc, bannerData.EndsAtUtc);
            }
            else
            {
                _banners.Add(new CategoryBanner(
                    Id, bannerData.ExternalBannerId, bannerData.ImageUrl, bannerData.MobileImageUrl,
                    bannerData.Title, bannerData.Subtitle, bannerData.Description,
                    bannerData.ButtonText, bannerData.ButtonUrl,
                    bannerData.DisplayOrder, bannerData.StartsAtUtc, bannerData.EndsAtUtc));
            }
        }
    }
}

public record CategorySyncData(
    long ExternalCategoryId,
    string NameEn,
    string NameAr,
    string? Slug,
    string? DescriptionEn,
    string? DescriptionAr,
    long? ParentId,
    string? ImageUrl,
    bool IsActive,
    string? VisibilityChannel,
    bool IsFeatured,
    int DisplayOrder,
    DateTime? EndsAtUtc,
    bool DisplayDescendantProducts,
    bool ShowChildCategories,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl);

public record CategoryBannerSyncData(
    long ExternalBannerId,
    string? ImageUrl,
    string? MobileImageUrl,
    string? Title,
    string? Subtitle,
    string? Description,
    string? ButtonText,
    string? ButtonUrl,
    int DisplayOrder,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc);