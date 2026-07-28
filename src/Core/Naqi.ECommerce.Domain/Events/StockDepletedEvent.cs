// src/Core/Naqi.ECommerce.Domain/Events/StockDepletedEvent.cs

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Events;

public class StockDepletedEvent : BaseDomainEvent
{
    public long ProductId { get; }

    public StockDepletedEvent(long productId)
    {
        ProductId = productId;
    }
}