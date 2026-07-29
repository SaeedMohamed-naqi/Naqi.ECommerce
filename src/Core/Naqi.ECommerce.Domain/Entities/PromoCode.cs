// src/Core/Naqi.ECommerce.Domain/Entities/PromoCode.cs
//
// Ports the legacy PromoCode/coupon concept from api/coupons/info.
//
// NOTE on the "id" field: the legacy C# DTO declared coupons.id as
// string, but the ACTUAL JSON response has a genuine number
// ("id": 5, not "id": "5"). Newtonsoft.Json (used by the old code)
// silently coerces numbers into strings on deserialization, which is why
// that worked there - System.Text.Json (used here) does NOT do that
// coercion by default and would throw. ExternalPromoId is modeled as
// `long` to match the real wire type, not the legacy DTO's declared type.

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class PromoCode : BaseAuditableEntity
{
    public long ExternalPromoId { get; private set; }
    public string Code { get; private set; } = string.Empty;

    // Raw discount type as the source sends it ("percentage", "fixed", ...).
    public string DiscountType { get; private set; } = string.Empty;
    public bool IsPercentage { get; private set; }
    public decimal Value { get; private set; }

    public decimal? MinOrderAmount { get; private set; }
    public int? MaxUsage { get; private set; }
    public int? UsedCount { get; private set; }
    public bool UsageExhausted { get; private set; }
    public bool IsActive { get; private set; } // !UsageExhausted, same as legacy promo.Active

    public DateTime ExpiresAtUtc { get; private set; }

    // Fields present in the current API response but not in the legacy
    // DTO - kept for admin visibility since they were already there.
    public string? UsageTypeLabel { get; private set; }
    public bool IsAvailableForUser { get; private set; }

    private PromoCode() { } // EF Core

    private PromoCode(
        long externalPromoId, string code, string discountType, decimal value,
        decimal? minOrderAmount, int? maxUsage, int? usedCount, bool usageExhausted,
        DateTime expiresAtUtc, string? usageTypeLabel, bool isAvailableForUser)
    {
        ExternalPromoId = externalPromoId;
        Code = code;
        DiscountType = discountType;
        IsPercentage = discountType == "percentage";
        Value = value;
        MinOrderAmount = minOrderAmount;
        MaxUsage = maxUsage;
        UsedCount = usedCount;
        UsageExhausted = usageExhausted;
        IsActive = !usageExhausted;
        ExpiresAtUtc = expiresAtUtc;
        UsageTypeLabel = usageTypeLabel;
        IsAvailableForUser = isAvailableForUser;
    }

    public static PromoCode CreateFromSync(PromoCodeSyncData data) => new(
        data.ExternalPromoId, data.Code, data.DiscountType, data.Value,
        data.MinOrderAmount, data.MaxUsage, data.UsedCount, data.UsageExhausted,
        data.ExpiresAtUtc, data.UsageTypeLabel, data.IsAvailableForUser);

    public void UpdateFromSync(PromoCodeSyncData data)
    {
        Code = data.Code;
        DiscountType = data.DiscountType;
        IsPercentage = data.DiscountType == "percentage";
        Value = data.Value;
        MinOrderAmount = data.MinOrderAmount;
        MaxUsage = data.MaxUsage;
        UsedCount = data.UsedCount;
        UsageExhausted = data.UsageExhausted;
        IsActive = !data.UsageExhausted;
        ExpiresAtUtc = data.ExpiresAtUtc;
        UsageTypeLabel = data.UsageTypeLabel;
        IsAvailableForUser = data.IsAvailableForUser;
        LastModifiedAtUtc = DateTime.UtcNow;
    }
}

public record PromoCodeSyncData(
    long ExternalPromoId,
    string Code,
    string DiscountType,
    decimal Value,
    decimal? MinOrderAmount,
    int? MaxUsage,
    int? UsedCount,
    bool UsageExhausted,
    DateTime ExpiresAtUtc,
    string? UsageTypeLabel,
    bool IsAvailableForUser);