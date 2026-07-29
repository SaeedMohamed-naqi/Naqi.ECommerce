// src/Core/Naqi.ECommerce.Domain/Entities/OfferGroup.cs
//
// A shared marketing campaign/badge (e.g. "Best Sellers") that many
// products can belong to at once - the middleware's offer_group_id
// recurs across products, so this is modeled like Category: a shared
// master row resolved-or-created during sync, not duplicated per product.

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class OfferGroup : BaseAuditableEntity
{
    public long ExternalOfferGroupId { get; private set; }
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? IconUrl { get; private set; }
    public string? Color { get; private set; }
    public bool IsBig { get; private set; }
    public DateTime? ExpireAtUtc { get; private set; }

    private OfferGroup() { }

    public OfferGroup(long externalOfferGroupId, string nameEn, string nameAr,
        string? iconUrl, string? color, bool isBig, DateTime? expireAtUtc)
    {
        ExternalOfferGroupId = externalOfferGroupId;
        NameEn = nameEn;
        NameAr = nameAr;
        IconUrl = iconUrl;
        Color = color;
        IsBig = isBig;
        ExpireAtUtc = expireAtUtc;
    }

    public void UpdateFromSync(string nameEn, string nameAr, string? iconUrl, string? color, bool isBig, DateTime? expireAtUtc)
    {
        NameEn = nameEn;
        NameAr = nameAr;
        IconUrl = iconUrl;
        Color = color;
        IsBig = isBig;
        ExpireAtUtc = expireAtUtc;
        LastModifiedAtUtc = DateTime.UtcNow;
    }
}