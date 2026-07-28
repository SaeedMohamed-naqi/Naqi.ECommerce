// src/Core/Naqi.ECommerce.Application/Localization/JsonStringLocalizerFactory.cs
//
// Naming convention mirrors resx for a familiar migration path:
//   IStringLocalizer<SharedResource>  ->  Resources/SharedResource.en.json
//                                         Resources/SharedResource.ar.json
//   IStringLocalizer<ProductsResource> -> Resources/ProductsResource.en.json
//                                         Resources/ProductsResource.ar.json
//
// TO ADD A NEW FEATURE'S STRINGS LATER:
//   1. Create a marker class, e.g. Resources/OrdersResource.cs (empty class)
//   2. Add Resources/OrdersResource.en.json + OrdersResource.ar.json
//   3. Inject IStringLocalizer<OrdersResource> wherever needed - no DI
//      changes required, the factory resolves it by convention.
//
// TO ADD A NEW LANGUAGE LATER:
//   1. Add the culture code to LocalizationDependencyInjection.SupportedCultures
//   2. Add a matching .json file for EVERY existing marker class
//      (SharedResource.fr.json, ProductsResource.fr.json, ...)
//   3. Nothing else changes.

using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Naqi.ECommerce.Application.Localization;

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly string _resourcesRootPath;

    public JsonStringLocalizerFactory(string resourcesRootPath)
    {
        _resourcesRootPath = resourcesRootPath;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var baseName = resourceSource.Name; // e.g. "SharedResource"
        return CreateLocalizer(baseName);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        // Strip namespace/path noise if passed in; we only care about the file's base name.
        var cleanBaseName = baseName.Contains('.')
            ? baseName[(baseName.LastIndexOf('.') + 1)..]
            : baseName;

        return CreateLocalizer(cleanBaseName);
    }

    private IStringLocalizer CreateLocalizer(string baseName)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var filePath = Path.Combine(_resourcesRootPath, $"{baseName}.{culture}.json");

        var strings = JsonStringLocalizer.LoadFromFile(filePath);

        // Fallback to default culture's file for any key missing in the
        // current culture, so a partially-translated language doesn't show
        // raw keys - merge default-culture strings underneath current ones.
        if (culture != LocalizationDependencyInjection.DefaultCulture)
        {
            var fallbackPath = Path.Combine(
                _resourcesRootPath, $"{baseName}.{LocalizationDependencyInjection.DefaultCulture}.json");
            var fallbackStrings = JsonStringLocalizer.LoadFromFile(fallbackPath);

            var merged = new Dictionary<string, string>(fallbackStrings);
            foreach (var kv in strings)
                merged[kv.Key] = kv.Value;

            return new JsonStringLocalizer(merged);
        }

        return new JsonStringLocalizer(strings);
    }
}