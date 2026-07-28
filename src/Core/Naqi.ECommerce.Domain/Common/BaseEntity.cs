// src/Core/Naqi.ECommerce.Domain/Common/BaseEntity.cs
//
// Root base class for every entity in the Domain layer. Uses a long
// (bigint identity) key - EF Core maps this to an auto-incrementing
// column by convention, so no Id generation logic is needed here.
// Note: this is independent of Identity's ApplicationUser/ApplicationRole
// keys, which use Guid - the two key spaces don't need to match.

namespace Naqi.ECommerce.Domain.Common;

public abstract class BaseEntity
{
    public long Id { get; protected set; }

    // [NotMapped] intentionally NOT referenced here to keep Domain free of
    // EF Core dependencies - instead, Infrastructure's ApplicationDbContext
    // calls modelBuilder.Ignore<BaseDomainEvent>() globally in
    // OnModelCreating, which achieves the same result without a package
    // reference leaking into the Domain project.
    private readonly List<BaseDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<BaseDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(BaseDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    protected void RemoveDomainEvent(BaseDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    // Equality by Id, not by reference - standard for entities vs value objects
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();

    public static bool operator ==(BaseEntity? left, BaseEntity? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(BaseEntity? left, BaseEntity? right) => !(left == right);
}