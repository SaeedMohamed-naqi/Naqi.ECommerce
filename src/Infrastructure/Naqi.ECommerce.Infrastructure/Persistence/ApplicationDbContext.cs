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

namespace Naqi.ECommerce.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    //public DbSet<Order> Orders => Set<Order>();
    //public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    //public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // MUST come first - configures Identity tables

        // BaseEntity.DomainEvents is a runtime-only collection (raised events
        // waiting to be dispatched after SaveChanges) - it is NOT persisted
        // data, so EF Core must not try to map BaseDomainEvent as an entity.
        builder.Ignore<Naqi.ECommerce.Domain.Common.BaseDomainEvent>();

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Rename default AspNetXxx Identity tables to something tidier (optional)
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("UserTokens");
    }
}