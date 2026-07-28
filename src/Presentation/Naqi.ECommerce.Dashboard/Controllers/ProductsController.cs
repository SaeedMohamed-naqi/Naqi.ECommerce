// src/Presentation/Naqi.ECommerce.Dashboard/Controllers/ProductsController.cs

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Naqi.ECommerce.Application.Common.Models;
using Naqi.ECommerce.Application.Features.Products.Commands;
using Naqi.ECommerce.Application.Features.Products.Queries;

namespace Naqi.ECommerce.Dashboard.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Just renders the page shell now - DataTables loads the actual rows
    // via GetData below, so no query is run here anymore.
    public IActionResult Index()
    {
        return View();
    }

    // DataTables server-side processing endpoint. DataTables POSTs its
    // paging/search/sort state as form fields using bracket notation
    // (draw, start, length, search[value], order[0][column], etc.) -
    // ASP.NET Core's default model binder doesn't understand that
    // bracket syntax out of the box, so these are read directly off
    // Request.Form instead of relying on action-parameter binding.
    [HttpPost]
    public async Task<IActionResult> GetData()
    {
        var form = Request.Form;

        var draw = ParseInt(form["draw"], 1);
        var start = ParseInt(form["start"], 0);
        var length = ParseInt(form["length"], 20);
        var searchValue = form["search[value]"].FirstOrDefault() ?? string.Empty;

        var page = (start / Math.Max(length, 1)) + 1;

        var result = await _mediator.Send(new GetProductsPagedQuery(page, length, searchValue));

        // DataTables expects this EXACT shape (camelCase matches ASP.NET
        // Core's default JSON naming policy, so no extra config needed):
        //   { draw, recordsTotal, recordsFiltered, data }
        return Json(new
        {
            draw,
            recordsTotal = result.OverallCount,
            recordsFiltered = result.TotalCount,
            data = result.Items
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync()
    {
        var result = await _mediator.Send(new SyncProductsCommand());

        if (!result.Success)
            return StatusCode(500, ApiResponse.Fail(result.ErrorMessage ?? "Sync failed."));

        var summary = new SyncProductsSummaryDto(result.TotalFetched, result.Created, result.Updated);
        return Ok(ApiResponse<SyncProductsSummaryDto>.Ok(summary));
    }

    private static int ParseInt(Microsoft.Extensions.Primitives.StringValues value, int fallback) =>
        int.TryParse(value.FirstOrDefault(), out var parsed) ? parsed : fallback;
}