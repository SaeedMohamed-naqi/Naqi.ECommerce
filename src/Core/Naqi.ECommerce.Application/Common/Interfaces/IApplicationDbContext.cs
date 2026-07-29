// src/Core/Naqi.ECommerce.Application/Common/Interfaces/IApplicationDbContext.cs
//
// Application layer depends on this abstraction, not on EF Core directly.
// Infrastructure's ApplicationDbContext implements it. Identity tables
// (Users/Roles) are deliberately NOT exposed here - Application code should
// never query Identity directly; use ICurrentUserService / UserManager
// injected where actually needed (e.g. Account controllers), keeping
// Identity as an Infrastructure concern.

using Microsoft.EntityFrameworkCore;
using Naqi.ECommerce.Domain.Entities;

namespace Naqi.ECommerce.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<ProductSpecification> ProductSpecifications { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<ProductInstallation> ProductInstallations { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<OfferGroup> OfferGroups { get; }
    DbSet<ProductOffer> ProductOffers { get; }
    DbSet<Category> Categories { get; }
    DbSet<CategoryBanner> CategoryBanners { get; }
     
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}