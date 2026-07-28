// src/Core/Naqi.ECommerce.Domain/Common/BaseDomainEvent.cs
//
// Marker base class for domain events raised by entities (e.g.
// OrderPlacedEvent, StockDepletedEvent). Dispatched by
// SaveChangesInterceptor/DispatchDomainEventsInterceptor in Infrastructure
// right before or after SaveChangesAsync.

using MediatR;

namespace Naqi.ECommerce.Domain.Common;

public abstract class BaseDomainEvent : INotification
{
    public DateTime OccurredOnUtc { get; protected set; } = DateTime.UtcNow;
}