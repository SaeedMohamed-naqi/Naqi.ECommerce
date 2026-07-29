// src/Core/Naqi.ECommerce.Application/Features/Categories/Queries/GetCategoryDetailsQuery.cs

using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Naqi.ECommerce.Application.Common.Interfaces;

namespace Naqi.ECommerce.Application.Features.Categories.Queries;

public record GetCategoryDetailsQuery(long Id) : IRequest<CategoryDetailsDto?>;

public record CategoryBannerDto(
    string? ImageUrl, string? MobileImageUrl, string? Title, string? Subtitle,
    string? Description, string? ButtonText, string? ButtonUrl,
    int DisplayOrder, DateTime? StartsAtUtc, DateTime? EndsAtUtc);

public record SubcategoryDto(long Id, string NameEn, string NameAr, bool IsActive);

public record CategoryDetailsDto(
    long Id,
    long? ExternalCategoryId,
    string NameEn,
    string NameAr,
    string? Slug,
    string? DescriptionEn,
    string? DescriptionAr,
    long? ParentId,
    string? ParentNameEn,
    string? ParentNameAr,
    string? ImageUrl,
    bool IsActive,
    string? VisibilityChannel,
    bool IsFeatured,
    int DisplayOrder,
    DateTime? EndsAtUtc,
    bool DisplayDescendantProducts,
    bool ShowChildCategories,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? CanonicalUrl,
    IReadOnlyList<CategoryBannerDto> Banners,
    IReadOnlyList<SubcategoryDto> Subcategories,
    int ProductCount,
    DateTime CreatedAtUtc,
    Guid? CreatedBy,
    DateTime? LastModifiedAtUtc,
    Guid? LastModifiedBy);

public class GetCategoryDetailsQueryHandler : IRequestHandler<GetCategoryDetailsQuery, CategoryDetailsDto?>
{
    private readonly IApplicationDbContext _context;

    public GetCategoryDetailsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<CategoryDetailsDto?> Handle(GetCategoryDetailsQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Parent)
            .Include(c => c.Banners)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null) return null;

        // Mapster flattens Parent.NameEn -> ParentNameEn, Parent.NameAr ->
        // ParentNameAr, and converts Banners (CategoryBanner entities) to
        // CategoryBannerDto automatically - see MappingConfig.cs.
        var dto = category.Adapt<CategoryDetailsDto>();

        var subcategories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.ParentId == category.Id)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new SubcategoryDto(c.Id, c.NameEn, c.NameAr, c.IsActive))
            .ToListAsync(cancellationToken);

        var productCount = await _context.Products
            .CountAsync(p => p.CategoryId == category.Id, cancellationToken);

        return dto with { Subcategories = subcategories, ProductCount = productCount };
    }
}