// src/Core/Naqi.ECommerce.Application/Common/Interfaces/NaqiMiddleware/MiddlewareActiveOffersDtos.cs
//
// Mirrors /api/get-active-offers. Each item wraps an offer_group (same
// shape as the one already nested inside product offers - reuses
// MiddlewareOfferGroup directly) plus a "products" array.
//
// The products array is intentionally NOT modeled here - the legacy
// GetOffers() action fetched it but never actually persisted it (it only
// ever saved offer_group as a "Brand"). Product-to-offer-group links are
// already handled by SyncProductsCommandHandler via each product's own
// "offers" array, which carries a real per-relationship offer_id this
// endpoint doesn't expose. Re-deriving links from here would need a
// different (and inconsistent) key scheme, so this sync stays scoped to
// the offer groups themselves, matching the old code's actual behavior.

namespace Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware;

public class MiddlewareActiveOffersResponse
{
    public List<MiddlewareActiveOfferItem> Data { get; set; } = new();
}

public class MiddlewareActiveOfferItem
{
    public MiddlewareOfferGroup OfferGroup { get; set; } = new();
}