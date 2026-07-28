// src/Presentation/Naqi.ECommerce.Dashboard/Controllers/HomeController.cs
//
// Post-login landing page. [Authorize] at the class level means every
// action here requires a signed-in user - this is what AccountController's
// RedirectToLocal() sends the user to after a successful login.
//
// Error() is the exception - it must be reachable by anyone, including
// unauthenticated users, since GlobalExceptionMiddleware can redirect here
// from anywhere in the pipeline (e.g. before auth even resolves).

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Naqi.ECommerce.Dashboard.Models;

namespace Naqi.ECommerce.Dashboard.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
    [Route("/Home/Error")]
    public IActionResult Error(int statusCode = 500)
    {
        Response.StatusCode = statusCode;

        return View(new ErrorViewModel
        {
            RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}