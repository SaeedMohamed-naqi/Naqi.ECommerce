// src/Presentation/Naqi.ECommerce.Api/Extensions/SwaggerServiceExtensions.cs
//
// Swagger/OpenAPI setup pulled out of Program.cs so that file stays
// readable as more concerns (rate limiting, health checks, versioning,
// etc.) get added over time - Program.cs just calls
// builder.Services.AddNaqiSwagger() and moves on.

using Microsoft.OpenApi.Models;
using Naqi.ECommerce.Api.Middleware;

namespace Naqi.ECommerce.Api.Extensions;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddNaqiSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Naqi ECommerce API",
                Version = "v1",
                Description = "Registration, guest checkout, and storefront endpoints for the Naqi ECommerce platform."
            });

            // "Authorize" button in Swagger UI - paste a token from
            // Register or ConfirmPhone's response here (just the raw
            // token, Swashbuckle adds the "Bearer " prefix itself), and
            // every "Try it out" call on an [Authorize]-protected
            // endpoint automatically includes it.
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the JWT token here (just the token itself - no need to type \"Bearer \" first)."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // OPTIONAL: shows controller/action XML doc comments
            // (/// <summary>...) as descriptions in Swagger UI. Requires
            // <GenerateDocumentationFile>true</GenerateDocumentationFile>
            // in the Api project's .csproj - silently skipped if that
            // file doesn't exist, so safe to leave in either way.
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    // Pipeline half - config-driven instead of hardcoded to
    // IsDevelopment(), so Swagger's availability is a deployment/config
    // decision, not a code decision. Controlled by:
    //
    //   "Swagger": {
    //     "Enabled": true,
    //     "BasicAuthUsername": "...",   // optional - if BOTH username
    //     "BasicAuthPassword": "..."    // and password are set, Swagger
    //   }                               // is gated behind Basic Auth
    //
    // Leaving Enabled=false (the production appsettings.json default) is
    // the actual protection - Swagger exposes your entire API surface
    // and request/response shapes, which you generally don't want
    // publicly discoverable. The Basic Auth option exists for cases
    // where you specifically need Swagger reachable in a production-like
    // environment (staging, or production itself for internal use) but
    // still gated behind a shared credential.
    public static WebApplication UseNaqiSwagger(this WebApplication app)
    {
        var swaggerSection = app.Configuration.GetSection("Swagger");
        var enabled = swaggerSection.GetValue<bool>("Enabled", app.Environment.IsDevelopment());

        if (!enabled)
            return app;

        var username = swaggerSection["BasicAuthUsername"];
        var password = swaggerSection["BasicAuthPassword"];

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            // Only guards /swagger routes - never touches the actual API
            // controllers, which keep using JWT bearer auth as normal.
            app.UseWhen(
                context => context.Request.Path.StartsWithSegments("/swagger"),
                branch => branch.UseMiddleware<SwaggerBasicAuthMiddleware>(username, password));
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            // Empty RoutePrefix makes Swagger UI the root page - hitting
            // the API's base URL in a browser lands directly on Swagger
            // instead of a blank 404, per "start with default swagger".
            options.RoutePrefix = string.Empty;
        });

        return app;
    }
}