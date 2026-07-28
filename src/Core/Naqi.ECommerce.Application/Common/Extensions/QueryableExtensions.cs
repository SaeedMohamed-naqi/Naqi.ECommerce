// src/Core/Naqi.ECommerce.Application/Common/Extensions/QueryableExtensions.cs
//
// One place for the count-then-skip-take pattern every paginated list
// query needs (Products, Orders, Customers, Categories...). Works on any
// IQueryable<T> - EF Core translates .CountAsync()/.ToListAsync() into a
// single COUNT query and a single paged SELECT, same as writing them out
// by hand, just without repeating the boilerplate in every handler.

using Microsoft.EntityFrameworkCore;
using Naqi.ECommerce.Application.Common.Models;

namespace Naqi.ECommerce.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedListAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}