// src/Core/Naqi.ECommerce.Domain/Entities/CategoryBanner.cs
//
// One entry from the middleware's category banners array - promotional
// banner images shown on a category page. Full per-category snapshot,
// same approach as ProductSpecification/ProductVariant/etc.
//
// Constructor is internal - entries are only ever created through
// Category.SyncBanners().

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class CategoryBanner : BaseEntity
{
    public long CategoryId { get; private set; }

    // Ties this row back to the middleware's banner id for re-sync upserts.
    public long ExternalBannerId { get; private set; }

    public string? ImageUrl { get; private set; }
    public string? MobileImageUrl { get; private set; }
    public string? Title { get; private set; }
    public string? Subtitle { get; private set; }
    public string? Description { get; private set; }
    public string? ButtonText { get; private set; }
    public string? ButtonUrl { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime? StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }

    private CategoryBanner() { } // EF Core

    internal CategoryBanner(
        long categoryId, long externalBannerId, string? imageUrl, string? mobileImageUrl,
        string? title, string? subtitle, string? description, string? buttonText, string? buttonUrl,
        int displayOrder, DateTime? startsAtUtc, DateTime? endsAtUtc)
    {
        CategoryId = categoryId;
        ExternalBannerId = externalBannerId;
        ImageUrl = imageUrl;
        MobileImageUrl = mobileImageUrl;
        Title = title;
        Subtitle = subtitle;
        Description = description;
        ButtonText = buttonText;
        ButtonUrl = buttonUrl;
        DisplayOrder = displayOrder;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    internal void UpdateFromSync(
        string? imageUrl, string? mobileImageUrl, string? title, string? subtitle,
        string? description, string? buttonText, string? buttonUrl,
        int displayOrder, DateTime? startsAtUtc, DateTime? endsAtUtc)
    {
        ImageUrl = imageUrl;
        MobileImageUrl = mobileImageUrl;
        Title = title;
        Subtitle = subtitle;
        Description = description;
        ButtonText = buttonText;
        ButtonUrl = buttonUrl;
        DisplayOrder = displayOrder;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }
}