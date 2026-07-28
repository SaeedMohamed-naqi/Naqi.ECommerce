// src/Core/Naqi.ECommerce.Application/DependencyInjection.Localization.cs
//
// Central place that defines WHICH cultures Naqi supports, and wires up
// the custom JSON-based localizer factory (see Localization/JsonStringLocalizer.cs)
// instead of resx.
//
// TO ADD A NEW LANGUAGE LATER:
//   1. Add its culture code to SupportedCultures below (e.g. "fr")
//   2. Add matching .json files (see Resources/ folder convention)
//   3. Nothing else changes - middleware, controllers, and views all pick
//      it up automatically since they read from this same list.
//
// REQUIRES on Naqi.ECommerce.Application:
//   dotnet add package Microsoft.Extensions.Localization.Abstractions
//   dotnet add package Microsoft.AspNetCore.Localization
// (both are standalone NuGet packages, safe to add to a class library -
// they don't pull in the full ASP.NET Core web host)

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Naqi.ECommerce.Application.Localization;
using System.Globalization;

namespace Naqi.ECommerce.Application;

public static class LocalizationDependencyInjection
{
    // Single source of truth for supported cultures across the whole solution.
    public static readonly string[] SupportedCultures = { "en", "ar" };
    public const string DefaultCulture = "en";

    /// <param name="resourcesRootPath">
    /// Absolute path to the folder containing *.{culture}.json files.
    /// Pass builder.Environment.ContentRootPath + "/Resources" from each
    /// presentation project's Program.cs - keeping this explicit (rather
    /// than inferred) avoids coupling this class library to IWebHostEnvironment.
    /// </param>
    public static IServiceCollection AddNaqiLocalization(
        this IServiceCollection services, string resourcesRootPath)
    {
        services.AddSingleton<IStringLocalizerFactory>(
            _ => new JsonStringLocalizerFactory(resourcesRootPath));

        services.AddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = SupportedCultures
                .Select(c => new CultureInfo(c))
                .ToList();

            options.DefaultRequestCulture = new RequestCulture(DefaultCulture);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;

            // Order matters: cookie first (explicit user choice), then
            // Accept-Language header (browser default) as fallback.
            options.RequestCultureProviders = new IRequestCultureProvider[]
            {
                new CookieRequestCultureProvider { CookieName = "Naqi.Culture" },
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });

        return services;
    }

    public static bool IsRtl(string cultureCode) =>
        cultureCode.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
}