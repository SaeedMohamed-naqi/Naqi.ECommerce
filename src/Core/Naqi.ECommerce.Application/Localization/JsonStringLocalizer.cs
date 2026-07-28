// src/Core/Naqi.ECommerce.Application/Localization/JsonStringLocalizer.cs
//
// Reads a flat OR nested JSON file into a dictionary and exposes it through
// the standard IStringLocalizer interface - so every place that already
// uses IStringLocalizer<SharedResource> / IViewLocalizer keeps working
// unchanged; only the underlying storage format is JSON instead of resx.
//
// Supports nested JSON for readability:
//   { "Account": { "InvalidCredentials": "..." } }
// accessed via localizer["Account:InvalidCredentials"]

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace Naqi.ECommerce.Application.Localization;

public class JsonStringLocalizer : IStringLocalizer
{
    private readonly IReadOnlyDictionary<string, string> _strings;

    public JsonStringLocalizer(IReadOnlyDictionary<string, string> strings)
    {
        _strings = strings;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var found = _strings.TryGetValue(name, out var value);
            return new LocalizedString(name, value ?? name, resourceNotFound: !found);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var found = _strings.TryGetValue(name, out var value);
            var formatted = found ? string.Format(value!, arguments) : name;
            return new LocalizedString(name, formatted, resourceNotFound: !found);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _strings.Select(kv => new LocalizedString(kv.Key, kv.Value, resourceNotFound: false));

    // ---- Static helper used by the factory to load + flatten a JSON file ----

    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> FileCache = new();

    public static IReadOnlyDictionary<string, string> LoadFromFile(string filePath)
    {
        return FileCache.GetOrAdd(filePath, path =>
        {
            if (!File.Exists(path))
                return new Dictionary<string, string>();

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);

            var result = new Dictionary<string, string>();
            Flatten(doc.RootElement, prefix: null, result);
            return result;
        });
    }

    // Clears the cache - call this if you build a "reload translations without
    // restarting the app" admin feature later.
    public static void ClearCache() => FileCache.Clear();

    private static void Flatten(JsonElement element, string? prefix, Dictionary<string, string> result)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix is null ? property.Name : $"{prefix}:{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                Flatten(property.Value, key, result);
            }
            else
            {
                result[key] = property.Value.GetString() ?? string.Empty;
            }
        }
    }
}