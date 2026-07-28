// src/Presentation/Naqi.ECommerce.Dashboard/Controllers/CultureController.cs
//
// Sets the culture cookie and redirects back. Works for ANY culture in
// LocalizationDependencyInjection.SupportedCultures - no per-language code
// here, so adding "fr" later needs zero changes to this controller.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Naqi.ECommerce.Application;

namespace Naqi.ECommerce.Dashboard.Controllers;

[AllowAnonymous]
public class CultureController : Controller
{
    [HttpGet]
    public IActionResult Set(string culture, string returnUrl = "/")
    {
        if (!LocalizationDependencyInjection.SupportedCultures.Contains(culture))
            culture = LocalizationDependencyInjection.DefaultCulture;

        Response.Cookies.Append(
            "Naqi.Culture", // must match CookieRequestCultureProvider.CookieName set in AddNaqiLocalization
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true // required, or GDPR-style cookie consent banners will strip it
            });

        return LocalRedirect(returnUrl);
    }
}