// src/Infrastructure/Naqi.ECommerce.Infrastructure/Persistence/ApplicationDbContextFactory.cs
//
// EF Core design-time tooling (dotnet ef migrations/database update) needs
// to construct ApplicationDbContext WITHOUT running the full app host and
// its DI container. Implementing IDesignTimeDbContextFactory<T> gives it a
// direct, explicit way to do that - this fixes the
// "Unable to resolve service for type DbContextOptions<...>" error
// regardless of what Program.cs does or doesn't wire up.
//
// This class is ONLY used by the `dotnet ef` CLI at design time - it is
// never called by the running application itself.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Naqi.ECommerce.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build configuration manually, pointing at the STARTUP project's
        // appsettings (Dashboard). dotnet ef is typically invoked from the
        // solution root, so we look there first, then fall back to the
        // Dashboard project folder directly.
        var basePath = Directory.GetCurrentDirectory();
        var dashboardPath = Path.Combine(
            basePath, "src", "Presentation", "Naqi.ECommerce.Dashboard");

        var effectiveBasePath = Directory.Exists(dashboardPath) ? dashboardPath : basePath;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(effectiveBasePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=.;Database=Naqi_ECommerce;Trusted_Connection=True;TrustServerCertificate=True;";
        // ^ fallback used only if config isn't found at design time - replace
        // with your real dev connection string if this path ever gets hit.

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString,
            sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}