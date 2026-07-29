// src/Core/Naqi.ECommerce.Application/Common/Models/DataTablesRequest.cs
//
// Strongly-typed shape for DataTables' server-side processing request.
// Lives in Application (not Dashboard) since it's a plain POCO with no
// ASP.NET Core MVC dependency - any presentation project's DataTable-backed
// endpoint can use it, not just the Dashboard. The actual population from
// DataTables' bracket-notation form fields (search[value], etc.) happens
// in Dashboard's DataTablesRequestModelBinder, which IS MVC-specific and
// stays there - this class only defines the shape.

namespace Naqi.ECommerce.Application.Common.Models;

public class DataTablesRequest
{
    public int Draw { get; init; } = 1;
    public int Start { get; init; }
    public int Length { get; init; } = 20;
    public string SearchValue { get; init; } = string.Empty;

    /// <summary>1-based page number, derived from Start/Length for MediatR queries.</summary>
    public int Page => (Start / Math.Max(Length, 1)) + 1;
}