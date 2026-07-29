// src/Presentation/Naqi.ECommerce.Dashboard/Controllers/CategoriesController.cs

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Naqi.ECommerce.Application.Common.Models;
using Naqi.ECommerce.Application.Features.Categories.Commands;
using Naqi.ECommerce.Application.Features.Categories.Queries;

namespace Naqi.ECommerce.Dashboard.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Read-only category details, opened by clicking a row in the Index table.
    public async Task<IActionResult> Details(long id)
    {
        var category = await _mediator.Send(new GetCategoryDetailsQuery(id));
        if (category is null)
            return NotFound();

        return View(category);
    }

    [HttpPost]
    public async Task<IActionResult> GetData(DataTablesRequest request)
    {
        var result = await _mediator.Send(new GetCategoriesPagedQuery(request));

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
        var result = await _mediator.Send(new SyncCategoriesCommand());

        if (!result.Success)
            return StatusCode(500, ApiResponse.Fail(result.ErrorMessage ?? "Sync failed."));

        var summary = new SyncCategoriesSummaryDto(result.TotalProcessed, result.Created, result.Updated);
        return Ok(ApiResponse<SyncCategoriesSummaryDto>.Ok(summary));
    }
}