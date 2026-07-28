// src/Presentation/Naqi.ECommerce.Api/Middleware/ExceptionHandlingMiddleware.cs
//
// Catches ValidationException (and other known exceptions) and converts
// them into consistent JSON responses for Next.js to consume.
// Register in Program.cs BEFORE app.UseAuthorization():
//     app.UseMiddleware<ExceptionHandlingMiddleware>();

using System.Net;
using System.Text.Json;
using Naqi.ECommerce.Application.Common.Exceptions;

namespace Naqi.ECommerce.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failure");
            await WriteResponse(context, HttpStatusCode.BadRequest, new
            {
                title = "Validation failed",
                status = 400,
                errors = ex.Errors
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteResponse(context, HttpStatusCode.NotFound, new
            {
                title = "Resource not found",
                status = 404,
                detail = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            await WriteResponse(context, HttpStatusCode.Forbidden, new
            {
                title = "Forbidden",
                status = 403,
                detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteResponse(context, HttpStatusCode.InternalServerError, new
            {
                title = "An unexpected error occurred",
                status = 500
            });
        }
    }

    private static Task WriteResponse(HttpContext context, HttpStatusCode statusCode, object payload)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}