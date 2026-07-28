// src/Presentation/Naqi.ECommerce.Dashboard/Localization/LocalizedDisplayMetadataProvider.cs
//
// ASP.NET Core's AddDataAnnotationsLocalization only auto-localizes
// VALIDATION error messages ([Required], [EmailAddress], etc.) - it never
// wires up [Display(Name = "...")] to go through IStringLocalizer. This
// provider closes that gap so Display names are localized automatically
// too, using the SAME DataAnnotationLocalizerProvider delegate already
// configured in Program.cs - one source of truth for both.
//
// With this registered, <label asp-for="Email"></label> (empty label,
// no manual @Localizer[...] needed) will render the localized text
// automatically, exactly like validation messages already do.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Naqi.ECommerce.Dashboard.Localization;

public class LocalizedDisplayMetadataProvider : IDisplayMetadataProvider
{
    private readonly IStringLocalizerFactory _stringLocalizerFactory;
    private readonly IOptions<MvcDataAnnotationsLocalizationOptions> _localizationOptions;

    public LocalizedDisplayMetadataProvider(
        IStringLocalizerFactory stringLocalizerFactory,
        IOptions<MvcDataAnnotationsLocalizationOptions> localizationOptions)
    {
        _stringLocalizerFactory = stringLocalizerFactory;
        _localizationOptions = localizationOptions;
    }

    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        var displayAttribute = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
        if (displayAttribute?.Name is null)
            return;

        // If ResourceType is explicitly set, the built-in resx mechanism
        // already handles it - don't double-localize.
        if (displayAttribute.ResourceType is not null)
            return;

        var provider = _localizationOptions.Value.DataAnnotationLocalizerProvider;
        if (provider is null)
            return;

        var containingType = context.Key.ContainerType ?? context.Key.ModelType;
        if (containingType is null)
            return;

        var localizer = provider(containingType, _stringLocalizerFactory);
        var rawName = displayAttribute.Name;

        // Lazily evaluated - runs at render time, after the current
        // request's culture is already set by UseRequestLocalization.
        context.DisplayMetadata.DisplayName = () => localizer[rawName];
    }
}