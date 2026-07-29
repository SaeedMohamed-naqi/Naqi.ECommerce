// src/Core/Naqi.ECommerce.Application/Common/Interfaces/NaqiMiddleware/MiddlewareCategoryDtos.cs
//
// Mirrors /api/v2/categories/tree's response - a recursive tree (each
// node has its own "children" array). Unlike products' onSalePrice/
// productQuantity, every field here is already proper snake_case, so no
// JsonPropertyName overrides are needed - the global SnakeCaseLower
// policy handles all of them by convention.

namespace Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware;

public class MiddlewareCategoryTreeResponse
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public List<MiddlewareCategoryNode> Data { get; set; } = new();
}

public class MiddlewareCategoryNode
{
    public long CategoryId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public long? ParentId { get; set; }
    public string? Image { get; set; }
    public bool IsActive { get; set; }
    public string? VisibilityChannel { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public bool DisplayDescendantProducts { get; set; }
    public bool ShowChildCategories { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public List<MiddlewareCategoryBanner> Banners { get; set; } = new();
    public List<MiddlewareCategoryNode> Children { get; set; } = new();
}

public class MiddlewareCategoryBanner
{
    public long Id { get; set; }
    public string? Image { get; set; }
    public string? MobileImage { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
}