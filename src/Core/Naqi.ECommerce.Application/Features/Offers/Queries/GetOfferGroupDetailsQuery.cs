// src/Core/Naqi.ECommerce.Application/Features/Offers/Queries/GetOfferGroupDetailsQuery.cs

using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Naqi.ECommerce.Application.Common.Interfaces;

namespace Naqi.ECommerce.Application.Features.Offers.Queries;

public record GetOfferGroupDetailsQuery(long Id) : IRequest<OfferGroupDetailsDto?>;

public record OfferProductDto(long Id, string NameEn, string NameAr, decimal Price, string? ImageUrl, bool IsActive);

public record OfferGroupDetailsDto(
    long Id,
    long ExternalOfferGroupId,
    string NameEn,
    string NameAr,
    string? IconUrl,
    string? Color,
    bool IsBig,
    DateTime? ExpireAtUtc,
    IReadOnlyList<OfferProductDto> Products,
    int ProductCount,
    DateTime CreatedAtUtc,
    Guid? CreatedBy,
    DateTime? LastModifiedAtUtc,
    Guid? LastModifiedBy);

public class GetOfferGroupDetailsQueryHandler : IRequestHandler<GetOfferGroupDetailsQuery, OfferGroupDetailsDto?>
{
    private readonly IApplicationDbContext _context;

    public GetOfferGroupDetailsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<OfferGroupDetailsDto?> Handle(GetOfferGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        var group = await _context.OfferGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (group is null) return null;

        // OfferGroup's own properties match OfferGroupDetailsDto 1:1 by
        // name, so Mapster's convention handles this without any explicit
        // config - Products/ProductCount are filled in separately below,
        // since they come from a different query (the ProductOffer join),
        // not from OfferGroup itself.
        var dto = group.Adapt<OfferGroupDetailsDto>();

        // Product.Offers is a configured navigation collection even though
        // ProductOffer has no reverse "Product" navigation - EF Core can
        // still translate this Any()-based filter into a correlated
        // EXISTS subquery from the Product side.
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.Offers.Any(o => o.OfferGroupId == group.Id))
            .OrderBy(p => p.NameEn)
            .Select(p => new OfferProductDto(
                p.Id, p.NameEn, p.NameAr, p.Price, p.ImageUrl,
                p.Offers.First(o => o.OfferGroupId == group.Id).IsActive))
            .ToListAsync(cancellationToken);

        return dto with { Products = products, ProductCount = products.Count };
    }
}