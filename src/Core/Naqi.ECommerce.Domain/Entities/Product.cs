// src/Core/Naqi.ECommerce.Domain/Entities/Product.cs
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
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    //public Guid CategoryId { get; private set; }
    //public Category Category { get; private set; } = null!;

    private Product() { } // EF Core needs a parameterless constructor

    public Product(string name, string sku, decimal price, int stockQuantity/*, Guid categoryId*/)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));

        Name = name;
        Sku = sku;
        Price = price;
        StockQuantity = stockQuantity;
        //CategoryId = categoryId;
    }

    public void UpdateDetails(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));

        Name = name;
        Price = price;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (quantity > StockQuantity)
            throw new InvalidOperationException($"Insufficient stock for product '{Name}'.");

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
}