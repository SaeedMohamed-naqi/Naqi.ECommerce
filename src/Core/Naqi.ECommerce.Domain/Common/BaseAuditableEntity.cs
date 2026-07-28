// src/Core/Naqi.ECommerce.Domain/Common/BaseAuditableEntity.cs
//
// Adds audit trail fields. CreatedBy/LastModifiedBy are Guid? because they
// reference the Identity ApplicationUser.Id, which is now a Guid too.
// Populated automatically by an EF Core SaveChanges interceptor in
// Infrastructure (via ICurrentUserService), not set manually in handlers.

namespace Naqi.ECommerce.Domain.Common;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }

    public DateTime? LastModifiedAtUtc { get; set; }
    public Guid? LastModifiedBy { get; set; }

    // Soft delete - e-commerce entities (Products, Categories) are usually
    // deactivated rather than hard-deleted, since Orders reference them
    // historically and can't lose that link.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}