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
    private readonly Naqi.ECommerce.Application.Common.Interfaces.ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        Naqi.ECommerce.Application.Common.Interfaces.ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductInstallation> ProductInstallations => Set<ProductInstallation>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<OfferGroup> OfferGroups => Set<OfferGroup>();
    public DbSet<ProductOffer> ProductOffers => Set<ProductOffer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryBanner> CategoryBanners => Set<CategoryBanner>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<Customer> Customers => Set<Customer>();
 

    // Auto-populates CreatedBy/CreatedAtUtc on insert and
    // LastModifiedBy/LastModifiedAtUtc on update, for every entity that
    // inherits BaseAuditableEntity. UserId is null when there's no
    // authenticated user (background jobs like SyncProductsCommandHandler,
    // or anything running outside an HTTP request) - CreatedBy/LastModifiedBy
    // are left null in that case rather than forced to some placeholder value.
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // MUST come first - configures Identity tables

        // BaseEntity.DomainEvents is a runtime-only collection (raised events
        // waiting to be dispatched after SaveChanges) - it is NOT persisted
        // data, so EF Core must not try to map BaseDomainEvent as an entity.
        builder.Ignore<Naqi.ECommerce.Domain.Common.BaseDomainEvent>();

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ---- Product -> ProductSpecification (private-field-backed collection) ----
        builder.Entity<Product>(entity =>
        {
            entity.HasMany(p => p.Specifications)
                .WithOne()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // deleting a Product removes its specs too

            // EF Core 6+ usually auto-detects the _specifications backing
            // field by naming convention, but being explicit here avoids
            // any ambiguity since Specifications has no public setter.
            entity.Metadata.FindNavigation(nameof(Product.Specifications))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);

            // ---- Product -> ProductCategory (ui_categories, same pattern) ----
            entity.HasMany(p => p.UiCategories)
                .WithOne()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata.FindNavigation(nameof(Product.UiCategories))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);

            // ---- Product -> ProductInstallation (product_installations, same pattern) ----
            entity.HasMany(p => p.Installations)
                .WithOne()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata.FindNavigation(nameof(Product.Installations))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);

            // ---- Product -> ProductVariant (variants, same pattern) ----
            entity.HasMany(p => p.Variants)
                .WithOne()
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Metadata.FindNavigation(nameof(Product.Variants))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);

            // ---- Product -> ProductOffer (thin join row, not a full snapshot) ----
            entity.HasMany(p => p.Offers)
                .WithOne()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // deleting a Product removes its offer links

            entity.Metadata.FindNavigation(nameof(Product.Offers))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        // ---- ProductOffer -> OfferGroup (shared reference data) ----
        builder.Entity<ProductOffer>(entity =>
        {
            entity.HasOne(o => o.OfferGroup)
                .WithMany()
                .HasForeignKey(o => o.OfferGroupId)
                .OnDelete(DeleteBehavior.Restrict); // never cascade-delete shared OfferGroup rows via a product offer
        });

        // ---- Customer -> ApplicationUser (cross-layer FK, no navigation) ----
        // Customer lives in Domain and must not reference ApplicationUser
        // (Infrastructure.Identity) directly - Clean Architecture's
        // dependency rule. HasOne<ApplicationUser>() with no navigation
        // expression on either side configures a real FK constraint at
        // the database level without either C# type needing to know about
        // the other. IsRequired(false) since guest customers have no
        // ApplicationUserId at all. Restrict (not Cascade) - deleting an
        // Identity user should never silently delete their order/customer
        // history.
        builder.Entity<Customer>(entity =>
        {
            entity.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<Customer>(c => c.ApplicationUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique even though nullable - SQL Server allows multiple
            // NULLs through a unique index (each NULL is treated as
            // distinct), so any number of guest customers can coexist,
            // but two Customer rows can never point at the same real
            // ApplicationUserId.
            entity.HasIndex(c => c.ApplicationUserId).IsUnique();

            // Supports looking a guest customer back up by the token
            // stored in their browser (see Customer.GuestToken).
            entity.HasIndex(c => c.GuestToken).IsUnique();
        });

        // ---- Indexes on sync lookup columns ----
        // Every sync handler does WHERE ExternalXxxId IN (...) against
        // these columns (SyncProductsCommandHandler, SyncCategoriesCommandHandler,
        // SyncOffersCommandHandler, SyncPromoCodesCommandHandler). None of
        // these are foreign keys, so EF Core's convention-based
        // auto-indexing (which only covers FK columns) never created an
        // index for them - every sync run was doing a full table scan on
        // these columns, and that scan gets slower as the catalog grows.
        // This is the most likely actual cause of the intermittent
        // "Execution Timeout Expired" errors during product sync, more
        // than the timeout value itself. Plain (non-unique) indexes only -
        // not enforcing uniqueness at the DB level, just speeding up the
        // lookup.
        builder.Entity<Product>().HasIndex(p => p.ExternalProductId);
        builder.Entity<Category>().HasIndex(c => c.ExternalCategoryId);
        builder.Entity<Category>().HasIndex(c => c.Slug);
        builder.Entity<OfferGroup>().HasIndex(g => g.ExternalOfferGroupId);
        builder.Entity<PromoCode>().HasIndex(p => p.ExternalPromoId);

        // ---- Category self-referencing hierarchy + banners ----
        builder.Entity<Category>(entity =>
        {
            // Self-referencing FK MUST use Restrict (or NoAction) - SQL
            // Server rejects a cascade path that could delete a row
            // through more than one route, and Category -> Category is
            // exactly that kind of cycle if left as Cascade.
            entity.HasOne(c => c.Parent)
                .WithMany()
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Banners)
                .WithOne()
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Cascade); // deleting a Category removes its own banners

            entity.Metadata.FindNavigation(nameof(Category.Banners))!
                .SetPropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        // ---- Global query filter: hide soft-deleted rows automatically ----
        // Applies to every entity inheriting BaseAuditableEntity: Product
        // and its five child collections (ProductSpecification,
        // ProductCategory, ProductInstallation, ProductVariant,
        // ProductOffer), plus PromoCode. Category and OfferGroup also
        // extend BaseAuditableEntity (for audit fields) and so pick up
        // this filter too, but nothing currently sets IsDeleted on them -
        // only Product/PromoCode sync actually uses soft delete right
        // now. CategoryBanner stays on plain BaseEntity (no soft delete,
        // no filter) since Category sync is out of scope for this.
        // Normal queries (list pages, Details pages, etc.) never see
        // soft-deleted rows without any extra .Where() needed at each
        // call site. Sync code that legitimately needs to see
        // soft-deleted rows (to restore them if they reappear) uses
        // .IgnoreQueryFilters() explicitly - see
        // SyncProductsCommandHandler/SyncPromoCodesCommandHandler.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(Domain.Common.BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var isDeletedProperty = System.Linq.Expressions.Expression.Property(
                parameter, nameof(Domain.Common.BaseAuditableEntity.IsDeleted));
            var notDeleted = System.Linq.Expressions.Expression.Not(isDeletedProperty);
            var lambda = System.Linq.Expressions.Expression.Lambda(notDeleted, parameter);

            builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }

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
}