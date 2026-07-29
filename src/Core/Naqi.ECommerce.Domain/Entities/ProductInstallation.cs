// src/Core/Naqi.ECommerce.Domain/Entities/ProductInstallation.cs
//
// One entry from the middleware's product_installations array - the
// installation/delivery options available for a product (e.g. "Free
// delivery and installation"), each with its own price and a flag for
// which one is the currently selected default. Modeled as a full
// per-product snapshot (same approach as ProductSpecification/ProductCategory)
// rather than a shared master-table join, since the middleware sends the
// complete title/price per row rather than just referencing a shared
// installation-type id.
//
// Constructor is internal - entries are only ever created through
// Product.SyncInstallations(), which owns add/update/remove as one
// operation, same as the other two sync methods.

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class ProductInstallation : BaseEntity
{
    public long ProductId { get; private set; }

    // Ties this row back to the middleware's installation_id for re-sync upserts.
    public long ExternalInstallationId { get; private set; }

    public string TitleEn { get; private set; } = string.Empty;
    public string TitleAr { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public bool IsSelected { get; private set; }

    private ProductInstallation() { } // EF Core

    internal ProductInstallation(
        long productId, long externalInstallationId,
        string titleEn, string titleAr, decimal price, bool isSelected)
    {
        ProductId = productId;
        ExternalInstallationId = externalInstallationId;
        TitleEn = titleEn;
        TitleAr = titleAr;
        Price = price;
        IsSelected = isSelected;
    }

    internal void UpdateFromSync(string titleEn, string titleAr, decimal price, bool isSelected)
    {
        TitleEn = titleEn;
        TitleAr = titleAr;
        Price = price;
        IsSelected = isSelected;
    }
}