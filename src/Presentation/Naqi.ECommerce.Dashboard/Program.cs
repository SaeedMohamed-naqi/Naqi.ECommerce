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
//builder.Services.AddNaqiLocalization(
//    Path.Combine(builder.Environment.ContentRootPath, "Resources")); // Arabic + English JSON files, extensible - see LocalizationDependencyInjection.SupportedCultures
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
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

builder.Services.AddOptions<MvcOptions>()
    .Configure<IStringLocalizerFactory, IOptions<MvcDataAnnotationsLocalizationOptions>>(
        (mvcOptions, factory, localizationOptions) =>
        {
            mvcOptions.ModelMetadataDetailsProviders.Add(
                new LocalizedDisplayMetadataProvider(factory, localizationOptions));
        });
builder.Services.AddControllersWithViews()
    .AddViewLocalization() // enables IViewLocalizer in .cshtml files
    .AddDataAnnotationsLocalization(options =>
    options.DataAnnotationLocalizerProvider = (type, factory) =>
        factory.Create(typeof(SharedResource)));
    //.AddDataAnnotationsLocalization(options =>
    //    options.DataAnnotationLocalizerProvider = (type, factory) =>
    //        factory.Create(typeof(Naqi.ECommerce.Application.Resources.SharedResource)));

var app = builder.Build();

// ---- Seed roles + default SuperAdmin user on startup ----
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
// Handles EVERY unhandled exception from here down the pipeline - see
// GlobalExceptionMiddleware for the AJAX-vs-full-page distinction.
// Placed after UseDeveloperExceptionPage so Development still gets the
// detailed stack-trace page instead of the friendly Error view/JSON.
//if (!app.Environment.IsDevelopment())
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
    pattern: "{controller=home}/{action=index}/{id?}"); // land on login when unauthenticated

app.Run();