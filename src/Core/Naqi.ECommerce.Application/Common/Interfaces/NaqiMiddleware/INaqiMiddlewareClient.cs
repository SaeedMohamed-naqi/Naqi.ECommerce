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
}