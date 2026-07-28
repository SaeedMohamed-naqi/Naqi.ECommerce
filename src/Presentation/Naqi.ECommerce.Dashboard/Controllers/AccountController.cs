// src/Presentation/Naqi.ECommerce.Dashboard/Controllers/AccountController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Naqi.ECommerce.Application.Resources;
using Naqi.ECommerce.Dashboard.ViewModels;
using Naqi.ECommerce.Infrastructure.Identity;

namespace Naqi.ECommerce.Dashboard.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _localizer = localizer;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string system = "admin")
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl, System = system });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, _localizer["InvalidCredentials"]);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in via {System} system.", model.Email, model.System);
            return RedirectToLocal(model.ReturnUrl, model.System);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out.", model.Email);
            ModelState.AddModelError(string.Empty, _localizer["AccountLockedOut"]);
            return View(model);
        }

        ModelState.AddModelError(string.Empty, _localizer["InvalidCredentials"]);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl, string system = "admin")
    {
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl!);

        // "system" is the segmented toggle from the login screen - adjust
        // these target areas/controllers to match your actual routing once
        // a B2B admin area (or equivalent) exists.
        return system switch
        {
            "b2b" => RedirectToAction("Index", "ContactRequests", new { area = "B2B" }),
            _ => RedirectToAction("Index", "Home")
        };
    }
}