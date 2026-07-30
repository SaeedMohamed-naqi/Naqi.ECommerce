// src/Infrastructure/Naqi.ECommerce.Infrastructure/Persistence/DatabaseMigrationExtensions.cs
//
// Applies any pending EF Core migrations automatically at startup,
// instead of requiring a manual `dotnet ef database update` before every
// deploy. Shared between Dashboard and Api since both use the SAME
// ApplicationDbContext/schema - either one calling this keeps the
// database current, whichever happens to start first.
//
// Config-driven (Database:AutoMigrate) rather than always-on: many teams
// deliberately disable automatic migrations in production (preferring a
// controlled, reviewed migration step in CI/CD instead of "whichever app
// instance boots first mutates the schema"), while wanting it always-on
// for local development/staging convenience. Defaults to true if the
// setting is missing entirely, since that's the more useful default for
// getting started - flip it to false explicitly once you have a real
// deployment pipeline that runs migrations as its own step.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Naqi.ECommerce.Infrastructure.Persistence;

namespace Naqi.ECommerce.Infrastructure;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateNaqiDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration");

        var autoMigrate = configuration.GetValue<bool>("Database:AutoMigrate", defaultValue: true);
        if (!autoMigrate)
        {
            logger.LogInformation("Database:AutoMigrate is false - skipping automatic migration.");
            return;
        }

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Database is up to date - no pending migrations.");
            return;
        }

        logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
            pending.Count, string.Join(", ", pending));

        // Deliberately NOT wrapped in try/catch - a failed migration means
        // the app would be running against a schema it doesn't match, and
        // failing fast at startup is far safer than limping along with
        // half-applied/missing tables and hitting confusing runtime errors later.
        await context.Database.MigrateAsync();

        logger.LogInformation("Database migration completed successfully.");
    }
}