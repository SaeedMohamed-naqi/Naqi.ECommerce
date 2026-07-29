// src/Presentation/Naqi.ECommerce.Dashboard/Controllers/OffersController.cs

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Naqi.ECommerce.Application.Common.Models;
using Naqi.ECommerce.Application.Features.Offers.Commands;
using Naqi.ECommerce.Application.Features.Offers.Queries;

namespace Naqi.ECommerce.Dashboard.Controllers;

[Authorize]
public class OffersController : Controller
{
    private readonly IMediator _mediator;

    public OffersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Read-only offer group details, opened by clicking a row in the Index table.
    public async Task<IActionResult> Details(long id)
    {
        var offerGroup = await _mediator.Send(new GetOfferGroupDetailsQuery(id));
        if (offerGroup is null)
            return NotFound();

        return View(offerGroup);
    }

    [HttpPost]
    public async Task<IActionResult> GetData(DataTablesRequest request)
    {
        var result = await _mediator.Send(new GetOfferGroupsPagedQuery(request));

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
        var result = await _mediator.Send(new SyncOffersCommand());

        if (!result.Success)
            return StatusCode(500, ApiResponse.Fail(result.ErrorMessage ?? "Sync failed."));

        var summary = new SyncOffersSummaryDto(result.TotalProcessed, result.Created, result.Updated);
        return Ok(ApiResponse<SyncOffersSummaryDto>.Ok(summary));
    }
}