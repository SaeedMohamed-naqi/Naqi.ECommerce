// src/Presentation/Naqi.ECommerce.Dashboard/Helpers/CultureHelper.cs
//
// Small static helper so views don't each recompute
// "var currentCulture = CultureInfo.CurrentUICulture..." themselves.
// Static (not injected) because it only reads the ambient
// CultureInfo.CurrentUICulture already set by UseRequestLocalization
// earlier in the pipeline - there's no per-request state to inject here,
// just a couple of convenience properties.

using System.Globalization;
using Naqi.ECommerce.Application;

namespace Naqi.ECommerce.Dashboard.Helpers;

public static class CultureHelper
{
    public static string CurrentCulture => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    public static bool IsRtl => LocalizationDependencyInjection.IsRtl(CurrentCulture);

    public static string Dir => IsRtl ? "rtl" : "ltr";
}