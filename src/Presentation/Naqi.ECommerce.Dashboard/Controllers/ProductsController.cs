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

    // Read-only product details, opened by clicking a row in the Index table.
    public async Task<IActionResult> Details(long id)
    {
        var product = await _mediator.Send(new GetProductDetailsQuery(id));
        if (product is null)
            return NotFound();

        return View(product);
    }

    // DataTablesRequest is bound by DataTablesRequestModelBinder and passed
    // straight into the query's constructor - no per-field unpacking here.
    [HttpPost]
    public async Task<IActionResult> GetData(DataTablesRequest request)
    {
        var result = await _mediator.Send(new GetProductsPagedQuery(request));

        return Json(new
        {
            draw = request.Draw,
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
}