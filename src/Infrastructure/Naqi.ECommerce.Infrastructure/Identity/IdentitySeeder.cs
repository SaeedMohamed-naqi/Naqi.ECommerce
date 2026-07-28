// src/Infrastructure/Naqi.ECommerce.Infrastructure/Identity/IdentitySeeder.cs
//
// Seeds the SuperAdmin role and a default SuperAdmin user if none exists yet.
// Credentials come from configuration (appsettings / environment / user
// secrets) - NEVER hardcode the password in source.
//
// Call this from Program.cs (Dashboard project) right after building the
// app, before app.Run():
//
//     using (var scope = app.Services.CreateScope())
//     {
//         await IdentitySeeder.SeedAsync(scope.ServiceProvider);
//     }

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Naqi.ECommerce.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        // ---- 1. Ensure roles exist ----
        foreach (var roleName in new[] { Roles.SuperAdmin, Roles.Admin, Roles.Manager })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
                logger.LogInformation("Created role {Role}", roleName);
            }
        }

        // ---- 2. Read default SuperAdmin credentials from config ----
        // appsettings.json / appsettings.Development.json:
        //   "SeedSuperAdmin": {
        //     "Email": "superadmin@naqi.sa",
        //     "UserName": "superadmin",
        //     "Password": "ChangeMe!123",
        //     "FullName": "Naqi Super Admin"
        //   }
        // In production, override Password via environment variable:
        //   SeedSuperAdmin__Password=<strong-generated-password>
        var email = config["SeedSuperAdmin:Email"] ?? "superadmin@naqi.sa";
        var userName = config["SeedSuperAdmin:UserName"] ?? "superadmin";
        var password = config["SeedSuperAdmin:Password"]
            ?? throw new InvalidOperationException(
                "SeedSuperAdmin:Password is not configured. Set it via appsettings or environment variable.");
        var fullName = config["SeedSuperAdmin:FullName"] ?? "Super Admin";

        // ---- 3. Create the user if it doesn't already exist ----
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            logger.LogInformation("SuperAdmin user already exists, skipping seed.");
            return;
        }

        var superAdmin = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };

        var result = await userManager.CreateAsync(superAdmin, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to seed SuperAdmin user: {Errors}", errors);
            throw new InvalidOperationException($"Failed to seed SuperAdmin user: {errors}");
        }

        await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
        logger.LogWarning(
            "SuperAdmin user seeded with email {Email}. CHANGE THE DEFAULT PASSWORD immediately after first login.",
            email);
    }
}