// src/Core/Naqi.ECommerce.Domain/Entities/Category.cs

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class Category : BaseAuditableEntity
{
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;

 
    public string? ExternalCategoryId { get; private set; }

    public string? ImageUrl { get; private set; }

    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private Category() { }

    public Category(string nameEn, string nameAr, string? externalCategoryId = null, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new ArgumentException("Category name (EN) is required.", nameof(nameEn));

        NameEn = nameEn;
        NameAr = nameAr;
        ExternalCategoryId = externalCategoryId;
        ImageUrl = imageUrl;
    }

    public void UpdateFromSync(string nameEn, string nameAr, string? imageUrl)
    {
        NameEn = nameEn;
        NameAr = nameAr;
        ImageUrl = imageUrl;
        LastModifiedAtUtc = DateTime.UtcNow;
    }
}