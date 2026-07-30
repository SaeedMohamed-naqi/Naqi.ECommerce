// src/Presentation/Naqi.ECommerce.Api/Middleware/SwaggerBasicAuthMiddleware.cs
//
// Minimal Basic Auth gate - only ever wired up in front of /swagger (see
// SwaggerServiceExtensions.UseNaqiSwagger), never in front of the actual
// API controllers, which keep using JWT bearer auth as normal. This
// exists purely so Swagger can be safely left reachable in a
// production-like environment without exposing your entire API surface
// (routes, request/response shapes, model names) to anyone who finds the URL.

using System.Text;

namespace Naqi.ECommerce.Api.Middleware;

public class SwaggerBasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _username;
    private readonly string _password;

    public SwaggerBasicAuthMiddleware(RequestDelegate next, string username, string password)
    {
        _next = next;
        _username = username;
        _password = password;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encoded = authHeader["Basic ".Length..].Trim();
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var separatorIndex = decoded.IndexOf(':');

                if (separatorIndex > 0)
                {
                    var providedUsername = decoded[..separatorIndex];
                    var providedPassword = decoded[(separatorIndex + 1)..];

                    if (providedUsername == _username && providedPassword == _password)
                    {
                        await _next(context);
                        return;
                    }
                }
            }
            catch (FormatException)
            {
                // malformed header - falls through to the 401 challenge below
            }
        }

        context.Response.Headers.WWWAuthenticate = "Basic realm=\"Swagger\"";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }
}