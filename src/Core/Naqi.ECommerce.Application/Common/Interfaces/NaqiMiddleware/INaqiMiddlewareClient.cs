// src/Core/Naqi.ECommerce.Application/Common/Interfaces/INaqiMiddlewareClient.cs

using Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware;

namespace Naqi.ECommerce.Application.Common.Interfaces;

public interface INaqiMiddlewareClient
{
    /// <summary>
    /// Fetches one page of products from the middleware.
    /// </summary>
    Task<MiddlewareProductsResponse> GetProductsPageAsync(int skip, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the entire category tree in one call (no pagination -
    /// matches the legacy GetCategories() action's behavior).
    /// </summary>
    Task<MiddlewareCategoryTreeResponse> GetCategoryTreeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all currently active offer groups in one call (no
    /// pagination - matches the legacy GetOffers() action's behavior).
    /// </summary>
    Task<MiddlewareActiveOffersResponse> GetActiveOffersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all active promo codes/coupons in one call (no pagination -
    /// matches the legacy GetPromoCodes() action's behavior).
    /// </summary>
    Task<MiddlewareCouponsResponse> GetCouponsAsync(CancellationToken cancellationToken = default);
}