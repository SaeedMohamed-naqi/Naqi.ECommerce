// src/Core/Naqi.ECommerce.Application/Features/Products/Queries/GetProductDetailsQuery.cs

using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Naqi.ECommerce.Application.Common.Interfaces;

namespace Naqi.ECommerce.Application.Features.Products.Queries;

public record GetProductDetailsQuery(long Id) : IRequest<ProductDetailsDto?>;

public record SpecificationDto(string TitleEn, string TitleAr, string ValueEn, string ValueAr);
public record UiCategoryDto(string NameEn, string NameAr, string? Slug, string? ImageUrl, bool IsPrimary, bool IsLeaf);

public record ProductDetailsDto(
    long Id,
    string NameEn,
    string NameAr,
    string? TitleEn,
    string? TitleAr,
    string? DescriptionEn,
    string? DescriptionAr,
    string Sku,
    long ExternalProductId,
    decimal Price,
    decimal? OldPrice,
    int StockQuantity,
    long CategoryId,
    string CategoryNameEn,
    string CategoryNameAr,
    string? ImageUrl,
    IReadOnlyList<string> AllImageUrls,
    IReadOnlyList<SpecificationDto> Specifications,
    IReadOnlyList<UiCategoryDto> UiCategories,
    string? TagEn,
    string? TagAr,
    string? SubtagEn,
    string? SubtagAr,
    string? TagColor,
    string? SubtagIconUrl,
    bool IsVertical,
    decimal RatingAverage,
    int TotalRating,
    string? WebsiteWarranty,
    string? WebsiteAccessories,
    string? WebsiteGuidelines,
    string? WebsiteOtherSpecs,
    DateTime? LastSyncedAtUtc,
    DateTime CreatedAtUtc,
    Guid? CreatedBy,
    DateTime? LastModifiedAtUtc,
    Guid? LastModifiedBy);

public class GetProductDetailsQueryHandler : IRequestHandler<GetProductDetailsQuery, ProductDetailsDto?>
{
    private readonly IApplicationDbContext _context;

    public GetProductDetailsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ProductDetailsDto?> Handle(GetProductDetailsQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Specifications)
            .Include(p => p.UiCategories)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null) return null;

        // Mapster flattens Category.NameEn -> CategoryNameEn and converts
        // the Specifications collection (ProductSpecification -> SpecificationDto)
        // automatically since property names align 1:1 - see MappingConfig.cs.
        var dto = product.Adapt<ProductDetailsDto>();

        // AllImageUrls needs the comma-separated string split into a list -
        // that's a real transformation, not a rename/flatten, so it stays
        // explicit rather than trying to force Mapster to do string parsing.
        var allImages = product.AllImageUrls?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(url => url.Trim())
            .Where(url => url.Length > 0)
            .ToList() ?? new List<string>();

        return dto with { AllImageUrls = allImages };
    }
}