// src/Infrastructure/Naqi.ECommerce.Infrastructure/Persistence/ApplicationDbContext.cs
//
// Inherits IdentityDbContext<ApplicationUser, ApplicationRole, Guid> so
// Users/Roles/Claims tables come free, alongside your normal
// Products/Orders/Customers tables in the SAME database, all keyed by Guid.

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Domain.Entities;
using Naqi.ECommerce.Infrastructure.Identity;
using Naqi.ECommerce.Infrastructure.Services;

namespace Naqi.ECommerce.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    private readonly Naqi.ECommerce.Application.Common.Interfaces.ICurrentUserService _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService=currentUserService;
    }
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // MUST come first - configures Identity tables

        // BaseEntity.DomainEvents is a runtime-only collection (raised events
        // waiting to be dispatched after SaveChanges) - it is NOT persisted
        // data, so EF Core must not try to map BaseDomainEvent as an entity.
        builder.Ignore<Naqi.ECommerce.Domain.Common.BaseDomainEvent>();

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ---- Force every DateTime to be treated as UTC on read ----
        // SQL Server's datetime2 has no concept of DateTimeKind - EF Core
        // always materializes DateTime values as Kind=Unspecified, even
        // though we only ever write DateTime.UtcNow (Kind=Utc). Without
        // this, System.Text.Json won't emit the "Z" suffix when
        // serializing (it only does that for Kind=Utc), so the browser's
        // `new Date(...)` treats the value as already-local and performs
        // NO timezone conversion - which is exactly the "still shows UTC"
        // symptom. This converter re-stamps Kind=Utc on every read,
        // restoring the "Z" suffix in JSON and letting NaqiDateTime.toLocal()
        // actually convert correctly in the browser.
        var utcConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableUtcConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
            v => v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtcConverter);
                }
            }
        }

        // Rename default AspNetXxx Identity tables to something tidier (optional)
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("UserTokens");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var userId = _currentUserService.UserId;

        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utcNow;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedAtUtc = utcNow;
                    entry.Entity.LastModifiedBy = userId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
     
}