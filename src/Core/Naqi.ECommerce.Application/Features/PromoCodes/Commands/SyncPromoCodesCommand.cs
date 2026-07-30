// src/Core/Naqi.ECommerce.Application/Features/PromoCodes/Commands/SyncPromoCodesCommand.cs
//
// Ports the legacy GetPromoCodes() action. Same bulk-loading approach as
// SyncOffersCommandHandler - one query to find existing promo codes,
// in-memory create/update, one SaveChangesAsync - instead of the legacy
// code's per-coupon check+save (PromoCode_Checkpromo_id + SaveNaqiPromoCodeBasicInfo
// called once per coupon in the loop).

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Domain.Entities;

namespace Naqi.ECommerce.Application.Features.PromoCodes.Commands;

public record SyncPromoCodesCommand : IRequest<SyncPromoCodesResult>;

public record SyncPromoCodesResult(int TotalProcessed, int Created, int Updated, bool Success, string? ErrorMessage = null);

public record SyncPromoCodesSummaryDto(int TotalProcessed, int Created, int Updated);

public class SyncPromoCodesCommandHandler : IRequestHandler<SyncPromoCodesCommand, SyncPromoCodesResult>
{
    private readonly INaqiMiddlewareClient _middlewareClient;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SyncPromoCodesCommandHandler> _logger;

    public SyncPromoCodesCommandHandler(
        INaqiMiddlewareClient middlewareClient, IApplicationDbContext context, ILogger<SyncPromoCodesCommandHandler> logger)
    {
        _middlewareClient = middlewareClient;
        _context = context;
        _logger = logger;
    }

    public async Task<SyncPromoCodesResult> Handle(SyncPromoCodesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _middlewareClient.GetCouponsAsync(cancellationToken);

            // Safety guard: an empty response is ambiguous between
            // "genuinely no active coupons" and a transient/broken API
            // response - skip the disappearance check entirely rather
            // than risk soft-deleting every promo code on a hiccup.
            if (response.Coupons.Count == 0)
                return new SyncPromoCodesResult(0, 0, 0, Success: true);

            // Bulk-load every existing PromoCode this response references,
            // in one query, instead of a query+save per coupon.
            // IgnoreQueryFilters() so a previously soft-deleted promo code
            // can be found and RESTORED if it becomes active again,
            // instead of creating a duplicate.
            var externalIds = response.Coupons.Select(c => c.Id).Distinct().ToList();

            var existingPromoCodes = (await _context.PromoCodes
                    .IgnoreQueryFilters()
                    .Where(p => externalIds.Contains(p.ExternalPromoId))
                    .ToListAsync(cancellationToken))
                .ToDictionary(p => p.ExternalPromoId);

            var created = 0;
            var updated = 0;

            foreach (var coupon in response.Coupons)
            {
                var syncData = new PromoCodeSyncData(
                    coupon.Id, coupon.Code, coupon.Type, coupon.Value,
                    coupon.MinOrderAmount, coupon.MaxUsage, coupon.UsedCount, coupon.UsageExhausted,
                    coupon.ExpiresAt.UtcDateTime, coupon.UsageTypeLabel, coupon.IsAvailableForUser);

                if (existingPromoCodes.TryGetValue(coupon.Id, out var existing))
                {
                    if (existing.IsDeleted)
                        existing.Restore(); // active again after disappearing from a previous sync

                    existing.UpdateFromSync(syncData);
                    updated++;
                }
                else
                {
                    var newPromoCode = PromoCode.CreateFromSync(syncData);
                    _context.PromoCodes.Add(newPromoCode);
                    existingPromoCodes[coupon.Id] = newPromoCode;
                    created++;
                }
            }

            // This endpoint returns only currently-active coupons - any
            // PromoCode still active in our DB but absent here is no
            // longer active at the source (expired/removed/etc.), so
            // soft-delete it rather than leaving stale data around.
            var disappeared = await _context.PromoCodes
                .Where(p => !externalIds.Contains(p.ExternalPromoId))
                .ToListAsync(cancellationToken);

            foreach (var promoCode in disappeared)
            {
                promoCode.SoftDelete(Naqi.ECommerce.Domain.Common.ChildCollectionSyncer.SyncNotPresentReason);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new SyncPromoCodesResult(response.Coupons.Count, created, updated, Success: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Promo code sync failed.");
            return new SyncPromoCodesResult(0, 0, 0, Success: false, ErrorMessage: ex.Message);
        }
    }
}