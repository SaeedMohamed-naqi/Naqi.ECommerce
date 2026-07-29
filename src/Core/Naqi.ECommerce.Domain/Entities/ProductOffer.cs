// src/Core/Naqi.ECommerce.Domain/Entities/ProductOffer.cs
//
// Links a Product to a shared OfferGroup - the per-product half of the
// offers relationship. Unlike Specifications/Variants/Installations
// (full snapshots), this row is intentionally thin: just the FK to the
// shared OfferGroup plus this product's own status flag, since the
// campaign's actual name/icon/color lives once on OfferGroup, not
// duplicated per product.
//
// Constructor is internal - entries are only ever created through
// Product.SyncOffers().

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class ProductOffer : BaseEntity
{
    public long ProductId { get; private set; }

    // Ties this row back to the middleware's offer_id for re-sync upserts.
    public long ExternalOfferId { get; private set; }

    public long OfferGroupId { get; private set; }
    public OfferGroup OfferGroup { get; private set; } = null!;

    public bool IsActive { get; private set; }

    private ProductOffer() { } // EF Core

    internal ProductOffer(long productId, long externalOfferId, long offerGroupId, bool isActive)
    {
        ProductId = productId;
        ExternalOfferId = externalOfferId;
        OfferGroupId = offerGroupId;
        IsActive = isActive;
    }

    internal void UpdateFromSync(long offerGroupId, bool isActive)
    {
        OfferGroupId = offerGroupId;
        IsActive = isActive;
    }
}