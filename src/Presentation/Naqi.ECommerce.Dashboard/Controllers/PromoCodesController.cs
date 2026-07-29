// src/Presentation/Naqi.ECommerce.Dashboard/Controllers/PromoCodesController.cs

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Naqi.ECommerce.Application.Common.Models;
using Naqi.ECommerce.Application.Features.PromoCodes.Commands;
using Naqi.ECommerce.Application.Features.PromoCodes.Queries;

namespace Naqi.ECommerce.Dashboard.Controllers;

[Authorize]
public class PromoCodesController : Controller
{
    private readonly IMediator _mediator;

    public PromoCodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetData(DataTablesRequest request)
    {
        var result = await _mediator.Send(new GetPromoCodesPagedQuery(request));

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
        var result = await _mediator.Send(new SyncPromoCodesCommand());

        if (!result.Success)
            return StatusCode(500, ApiResponse.Fail(result.ErrorMessage ?? "Sync failed."));

        var summary = new SyncPromoCodesSummaryDto(result.TotalProcessed, result.Created, result.Updated);
        return Ok(ApiResponse<SyncPromoCodesSummaryDto>.Ok(summary));
    }
}