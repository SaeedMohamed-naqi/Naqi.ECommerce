// src/Core/Naqi.ECommerce.Application/Features/Offers/Commands/SyncOffersCommand.cs
//
// Ports the legacy GetOffers() action: fetch every active offer group and
// upsert it. Same bulk-loading approach as SyncProductsCommandHandler -
// one query to find existing groups, in-memory create/update for the
// rest, one SaveChangesAsync - instead of a query+save per offer group.

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Domain.Entities;

namespace Naqi.ECommerce.Application.Features.Offers.Commands;

public record SyncOffersCommand : IRequest<SyncOffersResult>;

public record SyncOffersResult(int TotalProcessed, int Created, int Updated, bool Success, string? ErrorMessage = null);

public record SyncOffersSummaryDto(int TotalProcessed, int Created, int Updated);

public class SyncOffersCommandHandler : IRequestHandler<SyncOffersCommand, SyncOffersResult>
{
    private readonly INaqiMiddlewareClient _middlewareClient;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SyncOffersCommandHandler> _logger;

    public SyncOffersCommandHandler(
        INaqiMiddlewareClient middlewareClient, IApplicationDbContext context, ILogger<SyncOffersCommandHandler> logger)
    {
        _middlewareClient = middlewareClient;
        _context = context;
        _logger = logger;
    }

    public async Task<SyncOffersResult> Handle(SyncOffersCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _middlewareClient.GetActiveOffersAsync(cancellationToken);

            if (response.Data.Count == 0)
                return new SyncOffersResult(0, 0, 0, Success: true);

            // Bulk-load every existing OfferGroup this response references,
            // in one query, instead of one FirstOrDefaultAsync per item.
            var externalIds = response.Data.Select(i => i.OfferGroup.Id).Distinct().ToList();

            var existingGroups = (await _context.OfferGroups
                    .Where(g => externalIds.Contains(g.ExternalOfferGroupId))
                    .ToListAsync(cancellationToken))
                .ToDictionary(g => g.ExternalOfferGroupId);

            var created = 0;
            var updated = 0;

            foreach (var item in response.Data)
            {
                var group = item.OfferGroup;
                var expireAtUtc = group.ExpireAt?.UtcDateTime;

                if (existingGroups.TryGetValue(group.Id, out var existing))
                {
                    existing.UpdateFromSync(group.OfferNameEn, group.OfferNameAr, group.OfferIcon, group.OfferColor, group.IsBig, expireAtUtc);
                    updated++;
                }
                else
                {
                    var newGroup = new OfferGroup(group.Id, group.OfferNameEn, group.OfferNameAr, group.OfferIcon, group.OfferColor, group.IsBig, expireAtUtc);
                    _context.OfferGroups.Add(newGroup);
                    existingGroups[group.Id] = newGroup;
                    created++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new SyncOffersResult(response.Data.Count, created, updated, Success: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Offer group sync failed.");
            return new SyncOffersResult(0, 0, 0, Success: false, ErrorMessage: ex.Message);
        }
    }
}