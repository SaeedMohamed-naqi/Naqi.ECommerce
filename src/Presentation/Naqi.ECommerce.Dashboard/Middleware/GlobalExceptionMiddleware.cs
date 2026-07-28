// src/Presentation/Naqi.ECommerce.Dashboard/Middleware/GlobalExceptionMiddleware.cs
//
// Catches everything that escapes the MVC pipeline unhandled - including
// Application.Common.Exceptions.ValidationException thrown by the
// MediatR ValidationBehavior, since command handlers don't catch that
// themselves (see SyncProductsCommandHandler for the one place that does
// catch its own exceptions and return a typed result instead).
//
// Behavior differs by request type, since a Dashboard serves both:
//   - AJAX/fetch calls (e.g. products-sync.js) -> JSON using the shared
//     ApiResponse envelope, so client-side JS has one consistent shape
//     to handle regardless of which endpoint failed.
//   - Normal full-page browser requests -> redirect to /Home/Error,
//     which renders a proper localized error page instead of raw JSON.
//
// Register this BEFORE app.UseAuthentication()/UseAuthorization() but
// AFTER app.UseRouting(), and use it INSTEAD OF (not alongside)
// app.UseExceptionHandler("/Home/Error") for full control - see Program.cs.

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Naqi.ECommerce.Application.Common.Exceptions;
using Naqi.ECommerce.Application.Common.Models;

namespace Naqi.ECommerce.Dashboard.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failure on {Path}", context.Request.Path);
            await HandleAsync(context, HttpStatusCode.BadRequest,
                "One or more validation failures have occurred.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access on {Path}", context.Request.Path);
            await HandleAsync(context, HttpStatusCode.Forbidden, "You do not have permission to do that.", ex);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found on {Path}", context.Request.Path);
            await HandleAsync(context, HttpStatusCode.NotFound, "The requested item was not found.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await HandleAsync(context, HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again.", ex);
        }
    }

    private async Task HandleAsync(HttpContext context, HttpStatusCode statusCode, string friendlyMessage, Exception ex)
    {
        if (context.Response.HasStarted)
            throw ex; // can't do anything at this point - let it surface normally

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;

        if (IsAjaxRequest(context))
        {
            context.Response.ContentType = "application/json";

            var message = _environment.IsDevelopment() ? ex.Message : friendlyMessage;

            var response = ex is ValidationException validationEx
                ? new ApiResponse<IDictionary<string, string[]>>
                {
                    Success = false,
                    Message = message,
                    Data = validationEx.Errors
                }
                : (object)ApiResponse.Fail(message);

            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        // Full-page request - redirect to the Error page rather than
        // writing JSON into what the browser expects to be HTML.
        context.Response.Redirect($"/Home/Error?statusCode={(int)statusCode}");
    }

    private static bool IsAjaxRequest(HttpContext context)
    {
        return context.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
            || context.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}