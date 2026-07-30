// src/Core/Naqi.ECommerce.Domain/Common/ChildCollectionSyncer.cs
//
// Generic upsert-soft-delete-restore logic shared by every "sync this
// child collection from external data" method (Product.SyncSpecifications,
// SyncUiCategories, SyncInstallations, SyncVariants, SyncOffers, and
// Category.SyncBanners). Previously each of these called
// _collection.RemoveAll(...) to prune anything no longer present in the
// incoming data - that's a HARD delete: EF Core would generate a real
// DELETE statement for it. This replaces that with soft delete:
// anything missing from `incoming` gets marked deleted (kept in the
// table, hidden from normal queries via the global query filter) rather
// than actually removed - and if something reappears in a later sync,
// it's automatically restored instead of creating a duplicate row.
//
// Lives in Domain.Common (not Application) so Product/Category's own
// Sync* methods - which are Domain-layer business logic - can call it
// without Domain depending on anything outside itself.

namespace Naqi.ECommerce.Domain.Common;

public static class ChildCollectionSyncer
{
    public const string SyncNotPresentReason = "Sync: not exist in data";

    /// <summary>
    /// Upserts a child collection against incoming sync data, soft-deleting
    /// anything no longer present and restoring anything that reappears.
    /// </summary>
    /// <typeparam name="TChild">The child entity type (e.g. ProductSpecification) - must inherit BaseAuditableEntity for soft delete support.</typeparam>
    /// <typeparam name="TSyncData">The incoming data shape (e.g. ProductSpecificationSyncData).</typeparam>
    /// <param name="tracked">The entity's own in-memory backing list (e.g. Product's private _specifications field).</param>
    /// <param name="incoming">Fresh data from the sync source for this sync run.</param>
    /// <param name="externalIdOf">Extracts the external/source id from an incoming data item.</param>
    /// <param name="childExternalIdOf">Extracts the external/source id from an existing tracked child.</param>
    /// <param name="updateExisting">Applies incoming data onto an existing (possibly just-restored) child.</param>
    /// <param name="createNew">Constructs a brand-new child from incoming data.</param>
    /// <param name="deleteReason">Reason recorded on soft-deleted children - defaults to a generic "no longer present" message.</param>
    public static void Sync<TChild, TSyncData>(
        List<TChild> tracked,
        IEnumerable<TSyncData> incoming,
        Func<TSyncData, long> externalIdOf,
        Func<TChild, long> childExternalIdOf,
        Action<TChild, TSyncData> updateExisting,
        Func<TSyncData, TChild> createNew,
        string deleteReason = SyncNotPresentReason)
        where TChild : BaseAuditableEntity
    {
        var incomingList = incoming.ToList();
        var incomingIds = incomingList.Select(externalIdOf).ToHashSet();

        // Soft-delete anything currently active but no longer present in
        // this sync's data - NOT removed from `tracked`, just flagged.
        foreach (var child in tracked)
        {
            if (!child.IsDeleted && !incomingIds.Contains(childExternalIdOf(child)))
            {
                child.SoftDelete(deleteReason);
            }
        }

        foreach (var data in incomingList)
        {
            var externalId = externalIdOf(data);
            var existing = tracked.FirstOrDefault(c => childExternalIdOf(c) == externalId);

            if (existing is not null)
            {
                if (existing.IsDeleted)
                    existing.Restore(); // reappeared after being gone in a previous sync

                updateExisting(existing, data);
            }
            else
            {
                tracked.Add(createNew(data));
            }
        }
    }
}