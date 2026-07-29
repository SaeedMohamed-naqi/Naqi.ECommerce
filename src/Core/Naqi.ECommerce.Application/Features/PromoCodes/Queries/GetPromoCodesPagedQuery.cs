// src/Core/Naqi.ECommerce.Application/Features/PromoCodes/Queries/GetPromoCodesPagedQuery.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Naqi.ECommerce.Application.Common.Extensions;
using Naqi.ECommerce.Application.Common.Interfaces;

namespace Naqi.ECommerce.Application.Features.PromoCodes.Queries;

public record GetPromoCodesPagedQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PagedPromoCodesResult>
{
    public GetPromoCodesPagedQuery(Naqi.ECommerce.Application.Common.Models.DataTablesRequest request)
        : this(request.Page, request.Length, request.SearchValue) { }
}

public record PromoCodeListItemDto(
    long Id, long ExternalPromoId, string Code, string DiscountType, bool IsPercentage, decimal Value,
    decimal? MinOrderAmount, int? MaxUsage, int? UsedCount, bool IsActive, DateTime ExpiresAtUtc);

public record PagedPromoCodesResult(IReadOnlyList<PromoCodeListItemDto> Items, int TotalCount, int OverallCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class GetPromoCodesPagedQueryHandler : IRequestHandler<GetPromoCodesPagedQuery, PagedPromoCodesResult>
{
    private readonly IApplicationDbContext _context;

    public GetPromoCodesPagedQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PagedPromoCodesResult> Handle(GetPromoCodesPagedQuery request, CancellationToken cancellationToken)
    {
        var overallCount = await _context.PromoCodes.CountAsync(cancellationToken);

        var query = _context.PromoCodes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(p => p.Code.Contains(request.Search));
        }

        var projected = query
            .OrderByDescending(p => p.ExpiresAtUtc)
            .Select(p => new PromoCodeListItemDto(
                p.Id, p.ExternalPromoId, p.Code, p.DiscountType, p.IsPercentage, p.Value,
                p.MinOrderAmount, p.MaxUsage, p.UsedCount, p.IsActive, p.ExpiresAtUtc));

        var paged = await projected.ToPagedListAsync(request.Page, request.PageSize, cancellationToken);

        return new PagedPromoCodesResult(paged.Items, paged.TotalCount, overallCount, paged.Page, paged.PageSize);
    }
}