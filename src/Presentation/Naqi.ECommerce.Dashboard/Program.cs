// src/Presentation/Naqi.ECommerce.Dashboard/Program.cs

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Naqi.ECommerce.Application;
using Naqi.ECommerce.Application.Resources;
using Naqi.ECommerce.Dashboard.Localization;
using Naqi.ECommerce.Infrastructure;
using Naqi.ECommerce.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// ---- Layers ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddNaqiLocalization(
    Path.Combine(AppContext.BaseDirectory, "wwwroot", "Resources")); // Arabic + English JSON files, extensible - see LocalizationDependencyInjection.SupportedCultures

// ---- Cookie-based auth for the server-rendered dashboard ----
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.Name = "Naqi.Dashboard.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // enforce HTTPS in prod
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole(Roles.SuperAdmin));
    options.AddPolicy("AdminOrAbove", policy => policy.RequireRole(Roles.SuperAdmin, Roles.Admin));
});

// ---- SINGLE consolidated AddControllersWithViews call ----
// Previously this was split across THREE separate calls, none of which
// included the ModelBinderProviders registration below - that's exactly
// why DataTablesRequestModelBinderProvider.GetBinder never fired. Chained
// extension methods must all hang off the SAME AddControllersWithViews()
// call (or at least the options lambda needs to be on the call that
// actually configures MvcOptions) - splitting it across multiple calls
// like before doesn't merge the options lambdas together.
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0,
        new Naqi.ECommerce.Dashboard.ModelBinders.DataTablesRequestModelBinderProvider());
})
    .AddRazorRuntimeCompilation()
    .AddViewLocalization() // enables IViewLocalizer in .cshtml files
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource)));

// Closes the gap AddDataAnnotationsLocalization leaves open: makes
// [Display(Name = "...")] localize automatically too, reusing the same
// DataAnnotationLocalizerProvider configured above - see
// LocalizedDisplayMetadataProvider for details.
builder.Services.AddOptions<MvcOptions>()
    .Configure<IStringLocalizerFactory, IOptions<MvcDataAnnotationsLocalizationOptions>>(
        (mvcOptions, factory, localizationOptions) =>
        {
            mvcOptions.ModelMetadataDetailsProviders.Add(
                new LocalizedDisplayMetadataProvider(factory, localizationOptions));
        });

var app = builder.Build();

// ---- Seed roles + default SuperAdmin user on startup ----
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

// Handles EVERY unhandled exception from here down the pipeline - see
// GlobalExceptionMiddleware for the AJAX-vs-full-page distinction.
// IMPORTANT: this must stay INSIDE the `if (!IsDevelopment())` guard -
// your pasted version had the `if` commented out with the middleware
// registration left unconditional, meaning it now runs in Development
// too and would swallow the detailed stack-trace page from
// UseDeveloperExceptionPage() above. Restored the guard here.
if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<Naqi.ECommerce.Dashboard.Middleware.GlobalExceptionMiddleware>();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Must come before auth middleware and before anything that reads culture
var localizationOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseAuthentication(); // must come before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=home}/{action=index}/{id?}");

app.Run();