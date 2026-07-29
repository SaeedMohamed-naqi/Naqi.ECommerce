// src/Core/Naqi.ECommerce.Application/Features/Categories/Commands/SyncCategoriesCommand.cs
//
// Fetches the ENTIRE category tree in one call (no pagination, unlike
// products) and recursively upserts each node. Parent nodes are always
// processed (and saved) before their children in the source JSON, so a
// child's ParentId can always resolve to an already-persisted parent's
// internal Id by the time it's processed - this mirrors the depth-first
// order the middleware already returns the tree in.

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware;
using Naqi.ECommerce.Domain.Entities;

namespace Naqi.ECommerce.Application.Features.Categories.Commands;

public record SyncCategoriesCommand : IRequest<SyncCategoriesResult>;

public record SyncCategoriesResult(int TotalProcessed, int Created, int Updated, bool Success, string? ErrorMessage = null);

public record SyncCategoriesSummaryDto(int TotalProcessed, int Created, int Updated);

public class SyncCategoriesCommandHandler : IRequestHandler<SyncCategoriesCommand, SyncCategoriesResult>
{
    private readonly INaqiMiddlewareClient _middlewareClient;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SyncCategoriesCommandHandler> _logger;

    public SyncCategoriesCommandHandler(
        INaqiMiddlewareClient middlewareClient, IApplicationDbContext context, ILogger<SyncCategoriesCommandHandler> logger)
    {
        _middlewareClient = middlewareClient;
        _context = context;
        _logger = logger;
    }

    public async Task<SyncCategoriesResult> Handle(SyncCategoriesCommand request, CancellationToken cancellationToken)
    {
        var totalProcessed = 0;
        var totalCreated = 0;
        var totalUpdated = 0;

        try
        {
            var response = await _middlewareClient.GetCategoryTreeAsync(cancellationToken);

            if (!response.Status)
                return new SyncCategoriesResult(0, 0, 0, Success: false, ErrorMessage: response.Message ?? "Category sync failed.");

            foreach (var rootNode in response.Data)
            {
                var (processed, created, updated) = await ProcessNodeAsync(rootNode, parentInternalId: null, cancellationToken);
                totalProcessed += processed;
                totalCreated += created;
                totalUpdated += updated;
            }

            return new SyncCategoriesResult(totalProcessed, totalCreated, totalUpdated, Success: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Category sync failed after processing {Count} categories ({Created} created, {Updated} updated so far).",
                totalProcessed, totalCreated, totalUpdated);
            return new SyncCategoriesResult(totalProcessed, totalCreated, totalUpdated, Success: false, ErrorMessage: ex.Message);
        }
    }

    // Returns (processed, created, updated) instead of using ref/out
    // parameters - those aren't allowed on async methods.
    private async Task<(int processed, int created, int updated)> ProcessNodeAsync(
        MiddlewareCategoryNode node, long? parentInternalId, CancellationToken cancellationToken)
    {
        var existing = await _context.Categories
            .Include(c => c.Banners)
            .FirstOrDefaultAsync(c => c.ExternalCategoryId == node.CategoryId, cancellationToken);

        var syncData = new CategorySyncData(
            node.CategoryId, node.NameEn, node.NameAr, node.Slug,
            node.DescriptionEn, node.DescriptionAr, parentInternalId, node.Image,
            node.IsActive, node.VisibilityChannel, node.IsFeatured, node.DisplayOrder,
            node.EndsAt?.UtcDateTime, node.DisplayDescendantProducts, node.ShowChildCategories,
            node.MetaTitle, node.MetaDescription, node.MetaKeywords, node.CanonicalUrl);

        Category category;
        var wasCreated = existing is null;

        if (existing is not null)
        {
            existing.UpdateFromTreeSync(syncData);
            category = existing;
        }
        else
        {
            category = Category.CreateFromSync(syncData);
            _context.Categories.Add(category);
        }

        var bannerData = node.Banners.Select(b => new CategoryBannerSyncData(
            b.Id, b.Image, b.MobileImage, b.Title, b.Subtitle, b.Description,
            b.ButtonText, b.ButtonUrl, b.DisplayOrder, b.StartsAt?.UtcDateTime, b.EndsAt?.UtcDateTime));

        category.SyncBanners(bannerData);

        // Save now so category.Id is populated and available for this
        // node's children to reference as their ParentId.
        await _context.SaveChangesAsync(cancellationToken);

        var processed = 1;
        var created = wasCreated ? 1 : 0;
        var updated = wasCreated ? 0 : 1;

        foreach (var child in node.Children)
        {
            var (childProcessed, childCreated, childUpdated) = await ProcessNodeAsync(child, category.Id, cancellationToken);
            processed += childProcessed;
            created += childCreated;
            updated += childUpdated;
        }

        return (processed, created, updated);
    }
}