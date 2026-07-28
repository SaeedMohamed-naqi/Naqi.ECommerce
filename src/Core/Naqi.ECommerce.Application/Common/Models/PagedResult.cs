// src/Core/Naqi.ECommerce.Application/Common/Models/PagedResult.cs
//
// Generic pagination envelope, used by ToPagedListAsync<T>() below.
// Feature-specific results (like PagedProductsResult, which also needs
// an unfiltered OverallCount for DataTables) can still wrap this instead
// of duplicating Items/TotalCount/Page/PageSize/TotalPages themselves.

namespace Naqi.ECommerce.Application.Common.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}