// src/Core/Naqi.ECommerce.Application/Features/Offers/Queries/GetOfferGroupsPagedQuery.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Naqi.ECommerce.Application.Common.Extensions;
using Naqi.ECommerce.Application.Common.Interfaces;

namespace Naqi.ECommerce.Application.Features.Offers.Queries;

public record GetOfferGroupsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PagedOfferGroupsResult>
{
    public GetOfferGroupsPagedQuery(Naqi.ECommerce.Application.Common.Models.DataTablesRequest request)
        : this(request.Page, request.Length, request.SearchValue) { }
}

public record OfferGroupListItemDto(
    long Id, long ExternalOfferGroupId, string NameEn, string NameAr,
    string? IconUrl, string? Color, bool IsBig, DateTime? ExpireAtUtc, int ProductCount);

public record PagedOfferGroupsResult(IReadOnlyList<OfferGroupListItemDto> Items, int TotalCount, int OverallCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class GetOfferGroupsPagedQueryHandler : IRequestHandler<GetOfferGroupsPagedQuery, PagedOfferGroupsResult>
{
    private readonly IApplicationDbContext _context;

    public GetOfferGroupsPagedQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PagedOfferGroupsResult> Handle(GetOfferGroupsPagedQuery request, CancellationToken cancellationToken)
    {
        var overallCount = await _context.OfferGroups.CountAsync(cancellationToken);

        var query = _context.OfferGroups.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(g => g.NameEn.Contains(request.Search) || g.NameAr.Contains(request.Search));
        }

        var projected = query
            .OrderByDescending(g => g.ExpireAtUtc)
            .Select(g => new OfferGroupListItemDto(
                g.Id, g.ExternalOfferGroupId, g.NameEn, g.NameAr, g.IconUrl, g.Color, g.IsBig, g.ExpireAtUtc,
                _context.ProductOffers.Count(o => o.OfferGroupId == g.Id)));

        var paged = await projected.ToPagedListAsync(request.Page, request.PageSize, cancellationToken);

        return new PagedOfferGroupsResult(paged.Items, paged.TotalCount, overallCount, paged.Page, paged.PageSize);
    }
}