// src/Core/Naqi.ECommerce.Application/Common/Interfaces/NaqiMiddleware/MiddlewareCouponDtos.cs
//
// Mirrors /api/coupons/info. Every field is proper snake_case, so no
// JsonPropertyName overrides needed. NOTE: "id" is a genuine JSON number
// in the real response (not a string, despite the legacy DTO declaring
// it as one) - modeled as `long` here to match the actual wire shape.

namespace Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware;

public class MiddlewareCouponsResponse
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public List<MiddlewareCoupon> Coupons { get; set; } = new();
}

public class MiddlewareCoupon
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "percentage", "fixed", ...
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? MaxUsage { get; set; }
    public int? UsedCount { get; set; }
    public bool UsageExhausted { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int? UsageType { get; set; }
    public string? UsageTypeLabel { get; set; }
    public string? PerUserLimit { get; set; }
    public bool? UserAlreadyUsed { get; set; }
    public bool IsAvailableForUser { get; set; }
}