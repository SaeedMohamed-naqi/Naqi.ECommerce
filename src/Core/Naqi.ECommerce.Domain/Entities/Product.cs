// src/Core/Naqi.ECommerce.Domain/Entities/Product.cs
//
// Extended to support syncing from NaqiEcommerceMiddleware: ExternalProductId
// ties a local Product back to the middleware's product_id so re-syncing
// updates the same row instead of creating duplicates. Arabic name/price/
// quantity/image are also tracked since the admin table and future
// storefront both need them.
//
// Notice: no public setters on business fields. State changes go through
// methods that can enforce invariants and raise domain events - this is
// the "rich domain model" pattern Clean Architecture pushes for, versus
// the anemic "public get; set;" style typical of EF6/CRUD-first projects.

using Naqi.ECommerce.Domain.Common;
using Naqi.ECommerce.Domain.Events;

namespace Naqi.ECommerce.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;

    // Ties this row back to the middleware's product_id for re-sync upserts.
    public long ExternalProductId { get; private set; }  

    public decimal Price { get; private set; }
    public decimal? OldPrice { get; private set; }
    public int StockQuantity { get; private set; }

    public long CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    // First image URL from the comma-separated product_media list.
    public string? ImageUrl { get; private set; }

    public DateTime? LastSyncedAtUtc { get; private set; }

    private Product() { } // EF Core needs a parameterless constructor

    public Product(string nameEn, string nameAr, /*string sku, */decimal price, int stockQuantity, long categoryId)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new ArgumentException("Product name is required.", nameof(nameEn));
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));

        NameEn = nameEn;
        NameAr = nameAr;
        //Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
    }

    public void UpdateDetails(string nameEn, string nameAr, decimal price)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new ArgumentException("Product name is required.", nameof(nameEn));
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));

        NameEn = nameEn;
        NameAr = nameAr;
        Price = price;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (quantity > StockQuantity)
            throw new InvalidOperationException($"Insufficient stock for product '{NameEn}'.");

        StockQuantity -= quantity;

        if (StockQuantity == 0)
            AddDomainEvent(new StockDepletedEvent(Id));
    }

    public void Restock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        StockQuantity += quantity;
    }

    // Factory for creating a brand-new local Product from a middleware
    // record during sync (first time this external product is seen).
    public static Product CreateFromSync(
        long externalProductId, string nameEn, string nameAr, /*string sku,*/
        decimal price, decimal? oldPrice, int stockQuantity, long categoryId, string? imageUrl)
    {
        var product = new Product(nameEn, nameAr,/* sku,*/ price, stockQuantity, categoryId)
        {
            ExternalProductId = externalProductId,
            OldPrice = oldPrice,
            ImageUrl = imageUrl,
            LastSyncedAtUtc = DateTime.UtcNow
        };
        return product;
    }

    // Applies fresh data from the middleware onto an EXISTING local Product
    // (found by ExternalProductId) during re-sync.
    public void UpdateFromSync(
        string nameEn, string nameAr, decimal price, decimal? oldPrice,
        int stockQuantity, long categoryId, string? imageUrl)
    {
        NameEn = nameEn;
        NameAr = nameAr;
        Price = price;
        OldPrice = oldPrice;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
        ImageUrl = imageUrl;
        LastSyncedAtUtc = DateTime.UtcNow;
        LastModifiedAtUtc = DateTime.UtcNow;
    }
}