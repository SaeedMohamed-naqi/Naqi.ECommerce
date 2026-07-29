// src/Core/Naqi.ECommerce.Application/Features/Products/Queries/GetProductsPagedQuery.cs

using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Naqi.ECommerce.Application.Common.Extensions;
using Naqi.ECommerce.Application.Common.Interfaces;

namespace Naqi.ECommerce.Application.Features.Products.Queries;

public record GetProductsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PagedProductsResult>
{
    // Lets a controller call `new GetProductsPagedQuery(dataTablesRequest)`
    // directly instead of unpacking Page/Length/SearchValue by hand.
    public GetProductsPagedQuery(Naqi.ECommerce.Application.Common.Models.DataTablesRequest request)
        : this(request.Page, request.Length, request.SearchValue) { }
}

public record ProductListItemDto(
    long Id,
    string NameEn,
    string NameAr,
    long ExternalProductId,
    decimal Price,
    decimal? OldPrice,
    int StockQuantity,
    string CategoryNameEn,
    string? ImageUrl,
    DateTime? LastSyncedAtUtc);

public record PagedProductsResult(IReadOnlyList<ProductListItemDto> Items, int TotalCount, int OverallCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class GetProductsPagedQueryHandler : IRequestHandler<GetProductsPagedQuery, PagedProductsResult>
{
    private readonly IApplicationDbContext _context;

    public GetProductsPagedQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PagedProductsResult> Handle(GetProductsPagedQuery request, CancellationToken cancellationToken)
    {
        // Unfiltered total - needed separately for DataTables' recordsTotal
        // (vs recordsFiltered, which reflects the search below).
        var overallCount = await _context.Products.CountAsync(cancellationToken);

        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(p =>
                p.NameEn.Contains(request.Search) ||
                p.NameAr.Contains(request.Search) ||
                p.ExternalProductId.ToString().Contains(request.Search));
        }

        var projected = query
            .OrderByDescending(p => p.LastSyncedAtUtc)
            .Select(p => new ProductListItemDto(
                p.Id, p.NameEn, p.NameAr, p.ExternalProductId, p.Price, p.OldPrice,
                p.StockQuantity, p.Category.NameEn, p.ImageUrl, p.LastSyncedAtUtc));

        var paged = await projected.ToPagedListAsync(request.Page, request.PageSize, cancellationToken);

        return new PagedProductsResult(paged.Items, paged.TotalCount, overallCount, paged.Page, paged.PageSize);
    }
}