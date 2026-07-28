// src/Presentation/Naqi.ECommerce.Dashboard/ViewModels/LoginViewModel.cs
//
// IMPORTANT: Do NOT use ResourceType/ErrorMessageResourceType here.
// Those tell DataAnnotations to look for a compile-time static string
// property on the given type (the old resx-Designer-class pattern) -
// that bypasses IStringLocalizer entirely and fails since SharedResource
// is just an empty marker class, not a generated resx wrapper.
//
// Instead, use plain string keys for Name/ErrorMessage. Because
// Program.cs already calls .AddDataAnnotationsLocalization(options =>
// options.DataAnnotationLocalizerProvider = ...), MVC automatically runs
// these strings through IStringLocalizer<SharedResource> (our JSON-backed
// factory) at validation/render time - no extra wiring needed here.

using System.ComponentModel.DataAnnotations;

namespace Naqi.ECommerce.Dashboard.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "EmailRequired")]
    [EmailAddress(ErrorMessage = "EmailInvalid")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "PasswordRequired")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "RememberMe")]
    public bool RememberMe { get; set; }

    // Segmented "system" toggle from the design (e.g. "admin" vs "b2b").
    // UI-only distinction unless you want it to drive different post-login
    // redirects/areas - see AccountController.Login for where that's applied.
    public string System { get; set; } = "admin";

    public string? ReturnUrl { get; set; }
}