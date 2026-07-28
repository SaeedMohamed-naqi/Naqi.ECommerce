// src/Core/Naqi.ECommerce.Application/Common/Interfaces/INaqiMiddlewareClient.cs

using Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware;

namespace Naqi.ECommerce.Application.Common.Interfaces;

public interface INaqiMiddlewareClient
{
    /// <summary>
    /// Fetches one page of products from the middleware.
    /// </summary>
    Task<MiddlewareProductsResponse> GetProductsPageAsync(int skip, int count, CancellationToken cancellationToken = default);
}