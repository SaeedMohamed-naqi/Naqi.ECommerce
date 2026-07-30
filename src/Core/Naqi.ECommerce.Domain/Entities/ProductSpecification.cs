// src/Core/Naqi.ECommerce.Domain/Entities/ProductSpecification.cs
//
// A single spec row synced from the middleware (e.g. "Total Weight: 13.1").
// The source calls these fields titel_name_*/description_name_* - the
// second pair is actually the VALUE (e.g. "13.1", "220-240 V"), not a
// prose description, so this uses TitleEn/Ar (label) and ValueEn/Ar
// (value) instead of propagating the source's confusing naming.
//
// Constructor is internal - specs are only ever created through
// Product.SyncSpecifications(), never directly, since Product owns the
// whole collection and needs to control add/update/remove as one operation.

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class ProductSpecification : BaseAuditableEntity
{
    public long ProductId { get; private set; }

    // Ties this row back to the middleware's specification id for re-sync upserts.
    public long ExternalSpecificationId { get; private set; }

    public string TitleEn { get; private set; } = string.Empty;
    public string TitleAr { get; private set; } = string.Empty;
    public string ValueEn { get; private set; } = string.Empty;
    public string ValueAr { get; private set; } = string.Empty;

    private ProductSpecification() { } // EF Core

    internal ProductSpecification(
        long productId, long externalSpecificationId,
        string titleEn, string titleAr, string valueEn, string valueAr)
    {
        ProductId = productId;
        ExternalSpecificationId = externalSpecificationId;
        TitleEn = titleEn;
        TitleAr = titleAr;
        ValueEn = valueEn;
        ValueAr = valueAr;
    }

    internal void UpdateFromSync(string titleEn, string titleAr, string valueEn, string valueAr)
    {
        TitleEn = titleEn;
        TitleAr = titleAr;
        ValueEn = valueEn;
        ValueAr = valueAr;
    }
}