// src/Infrastructure/Naqi.ECommerce.Infrastructure/ExternalServices/NaqiMiddleware/NaqiMiddlewareClient.cs
//
// Ports the token/auth pattern from the old ApiAuth-based controller:
// a "key" header built from a timestamp + shared secret, POSTed with
// {skip, count} to page through results. Registered via typed HttpClient
// (see DependencyInjection.cs) so the base address + timeout are
// configured centrally instead of `new HttpClient()` per call.

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware;

namespace Naqi.ECommerce.Infrastructure.ExternalServices.NaqiMiddleware;

public class NaqiMiddlewareClient : INaqiMiddlewareClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiToken;
    private readonly JsonSerializerOptions _jsonOptions;

    public NaqiMiddlewareClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiToken = configuration["NaqiMiddleware:ApiToken"]
            ?? throw new InvalidOperationException("NaqiMiddleware:ApiToken is not configured.");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            //Converters = { new LenientStringConverter() } // still useful for any genuinely string-typed field that occasionally arrives as a bare number
        };
    }

    public async Task<MiddlewareProductsResponse> GetProductsPageAsync(
        int skip, int count, CancellationToken cancellationToken = default)
    {
        // Same token shape as the legacy controller: "d@M@yyyy@H@m@s@{secret}"
        var token = $"{DateTime.Now:d@M@yyyy@H@m@s}@{_apiToken}";
        _httpClient.DefaultRequestHeaders.Remove("key");
        _httpClient.DefaultRequestHeaders.Add("key", token);

        var requestBody = new { skip, count };

        using var response = await _httpClient.PostAsJsonAsync(
            "api/v2/products", requestBody, _jsonOptions, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MiddlewareProductsResponse>(
            _jsonOptions, cancellationToken);

        return result ?? new MiddlewareProductsResponse { Status = false, Data = new() };
    }

    public async Task<MiddlewareCategoryTreeResponse> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
    {
        var token = $"{DateTime.Now:d@M@yyyy@H@m@s}@{_apiToken}";
        _httpClient.DefaultRequestHeaders.Remove("key");
        _httpClient.DefaultRequestHeaders.Add("key", token);

        // Legacy code POSTs an empty body here (no skip/count) - the tree
        // endpoint returns everything in one call, unlike products' paging.
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("api/v2/categories/tree", content, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MiddlewareCategoryTreeResponse>(
            _jsonOptions, cancellationToken);

        return result ?? new MiddlewareCategoryTreeResponse { Status = false, Data = new() };
    }
}