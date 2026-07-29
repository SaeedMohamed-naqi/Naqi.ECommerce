// src/Core/Naqi.ECommerce.Application/Features/Categories/Queries/GetCategoriesPagedQuery.cs

using MediatR;
using Microsoft.EntityFrameworkCore;
using Naqi.ECommerce.Application.Common.Extensions;
using Naqi.ECommerce.Application.Common.Interfaces;

namespace Naqi.ECommerce.Application.Features.Categories.Queries;

public record GetCategoriesPagedQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PagedCategoriesResult>
{
    public GetCategoriesPagedQuery(Naqi.ECommerce.Application.Common.Models.DataTablesRequest request)
        : this(request.Page, request.Length, request.SearchValue) { }
}

public record CategoryListItemDto(
    long Id,
    long? ExternalCategoryId,
    string NameEn,
    string NameAr,
    string? ParentNameEn,
    bool IsActive,
    bool IsFeatured,
    int DisplayOrder,
    string? ImageUrl);

public record PagedCategoriesResult(IReadOnlyList<CategoryListItemDto> Items, int TotalCount, int OverallCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class GetCategoriesPagedQueryHandler : IRequestHandler<GetCategoriesPagedQuery, PagedCategoriesResult>
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesPagedQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PagedCategoriesResult> Handle(GetCategoriesPagedQuery request, CancellationToken cancellationToken)
    {
        var overallCount = await _context.Categories.CountAsync(cancellationToken);

        var query = _context.Categories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(c =>
                c.NameEn.Contains(request.Search) ||
                c.NameAr.Contains(request.Search));
        }

        var projected = query
            .OrderByDescending(c => c.IsFeatured)
            .ThenBy(c => c.DisplayOrder)
            .Select(c => new CategoryListItemDto(
                c.Id, c.ExternalCategoryId, c.NameEn, c.NameAr,
                c.Parent != null ? c.Parent.NameEn : null,
                c.IsActive, c.IsFeatured, c.DisplayOrder, c.ImageUrl)) ;

        var paged = await projected.ToPagedListAsync(request.Page, request.PageSize, cancellationToken);

        return new PagedCategoriesResult(paged.Items, paged.TotalCount, overallCount, paged.Page, paged.PageSize);
    }
}