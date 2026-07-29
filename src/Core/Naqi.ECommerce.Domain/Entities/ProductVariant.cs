// src/Core/Naqi.ECommerce.Domain/Entities/ProductVariant.cs
//
// One entry from the middleware's variants array - a color/price/stock
// variation of a product (e.g. different color options), each carrying
// its own name, price, quantity, and media, same as the parent Product.
// Modeled as a full per-product snapshot, same approach as
// ProductSpecification/ProductCategory/ProductInstallation.
//
// Constructor is internal - entries are only ever created through
// Product.SyncVariants(), which owns add/update/remove as one operation.

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public long ProductId { get; private set; }

    // Ties this row back to the middleware's variant id for re-sync upserts.
    public long ExternalVariantId { get; private set; }

    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? ColorEn { get; private set; }
    public string? ColorAr { get; private set; }
    public string? ColorCode { get; private set; } // hex, e.g. "808080"
    public decimal Price { get; private set; }
    public decimal? OldPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public string? ImageUrl { get; private set; }

    private ProductVariant() { } // EF Core

    internal ProductVariant(
        long productId, long externalVariantId, string nameEn, string nameAr,
        string? colorEn, string? colorAr, string? colorCode,
        decimal price, decimal? oldPrice, int stockQuantity, string? imageUrl)
    {
        ProductId = productId;
        ExternalVariantId = externalVariantId;
        NameEn = nameEn;
        NameAr = nameAr;
        ColorEn = colorEn;
        ColorAr = colorAr;
        ColorCode = colorCode;
        Price = price;
        OldPrice = oldPrice;
        StockQuantity = stockQuantity;
        ImageUrl = imageUrl;
    }

    internal void UpdateFromSync(
        string nameEn, string nameAr, string? colorEn, string? colorAr, string? colorCode,
        decimal price, decimal? oldPrice, int stockQuantity, string? imageUrl)
    {
        NameEn = nameEn;
        NameAr = nameAr;
        ColorEn = colorEn;
        ColorAr = colorAr;
        ColorCode = colorCode;
        Price = price;
        OldPrice = oldPrice;
        StockQuantity = stockQuantity;
        ImageUrl = imageUrl;
    }
}