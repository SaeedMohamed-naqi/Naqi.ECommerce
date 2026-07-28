// src/Infrastructure/Naqi.ECommerce.Infrastructure/DependencyInjection.cs
//
// Registers EF Core + Identity for the Infrastructure layer.
// Called from both Api/Program.cs and Dashboard/Program.cs via:
//     builder.Services.AddInfrastructure(builder.Configuration);
//
// NOTE: This registers Identity's core services (UserManager, RoleManager,
// SignInManager) but NOT the authentication scheme (cookie vs JWT) - that
// stays in each presentation project's Program.cs since Api uses JWT and
// Dashboard uses cookies against the same Identity store.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Infrastructure.Identity;
using Naqi.ECommerce.Infrastructure.Persistence;

namespace Naqi.ECommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // ---- Identity core (shared by Api's JWT auth and Dashboard's cookie auth) ----
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password policy - tune to Naqi's security requirements
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                // Lockout policy - protects the SuperAdmin/login endpoint from brute force
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}