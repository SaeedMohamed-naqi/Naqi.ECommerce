// src/Core/Naqi.ECommerce.Application/Common/Interfaces/NaqiMiddleware/MiddlewareProductDtos.cs
//
// Mirrors the JSON shape returned by NaqiEcommerceMiddleware's
// /api/v2/products endpoint.
//
// IMPORTANT casing note: the global JsonSerializerOptions uses
// PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower (see
// NaqiMiddlewareClient), which auto-converts a PascalCase C# property
// name into snake_case (e.g. ProductNameEn -> product_name_en) - this
// covers most fields for free. But a few JSON keys are genuinely
// camelCase with NO underscore (onSalePrice, productQuantity) - the
// naming policy would generate "on_sale_price"/"product_quantity" for
// those, which does NOT match the real key even with
// PropertyNameCaseInsensitive=true (case-insensitivity doesn't add/remove
// underscores). Those need an explicit [JsonPropertyName] override pinning
// the literal wire name - already applied below for both.

namespace Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware;

public class MiddlewareProductsResponse
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public int TotalProducts { get; set; }
    public MiddlewarePagination? Pagination { get; set; }
    public List<MiddlewareProduct> Data { get; set; } = new();
}

public class MiddlewarePagination
{
    public int Skip { get; set; }
    public int Count { get; set; }
}

public class MiddlewareProduct
{
    public long ProductId { get; set; }
    public string ProductNameEn { get; set; } = string.Empty;
    public string ProductNameAr { get; set; } = string.Empty;
    public string? ProductTitleEn { get; set; }
    public string? ProductTitleAr { get; set; }
    public string? ProductDescriptionEn { get; set; }
    public string? ProductDescriptionAr { get; set; }
    public decimal ProductPrice { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("onSalePrice")]
    public decimal OnSalePrice { get; set; }

    public string? ProductMedia { get; set; } // comma-separated URLs - first one is the thumbnail, all of them go to AllImageUrls
    public int Quantity { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("productQuantity")]
    public MiddlewareProductQuantity? ProductQuantity { get; set; }

    public List<MiddlewareVariant>? Variants { get; set; }
    public List<MiddlewareUiCategory>? UiCategories { get; set; }
    public List<MiddlewareSpecification>? Specifications { get; set; }
    public List<MiddlewareProductInstallation>? ProductInstallations { get; set; }
    public List<MiddlewareOffer>? Offers { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // ---- Tag/subtag (app display) ----
    public string? Tag { get; set; }
    public string? TagAr { get; set; }
    public string? Subtag { get; set; }
    public string? SubtagAr { get; set; }
    public string? TagColor { get; set; }
    public string? Subtagicon { get; set; }

    public int IsVertical { get; set; } // 1/0 in JSON - converted to bool during upsert

    // The source JSON key is genuinely misspelled ("rating_avarage") -
    // JsonPropertyName pins the exact wire name while keeping the correct
    // spelling on the C# side.
    [System.Text.Json.Serialization.JsonPropertyName("rating_avarage")]
    public decimal RatingAverage { get; set; }
    public int TotalRating { get; set; }

    // ---- Website-only long-form content ----
    public string? WebsiteWarranty { get; set; }
    public string? WebsiteAccessories { get; set; }
    public string? WebsiteGuidelines { get; set; }
    public string? WebsiteOtherSpecs { get; set; }
}

public class MiddlewareProductQuantity
{
    public long ProductId { get; set; }
    public int Qnty { get; set; }
}

public class MiddlewareVariant
{
    public long ProductId { get; set; }
    public long Id { get; set; }
    public string? ColorEn { get; set; }
    public string? ColorAr { get; set; }
    public string? ColorCode { get; set; }
    public decimal ProductPrice { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("onSalePrice")]
    public decimal OnSalePrice { get; set; }

    public int ProductQnty { get; set; }
    public string? ProductMedia { get; set; }
    public string ProductNameEn { get; set; } = string.Empty;
    public string ProductNameAr { get; set; } = string.Empty;
}

public class MiddlewareUiCategory
{
    public long CategoryId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public bool IsLeaf { get; set; }
    public bool IsPrimary { get; set; }
    public string? Image { get; set; }
}

public class MiddlewareSpecification
{
    public long Id { get; set; }
    public long ProductId { get; set; }

    // Source's own naming ("titel_name_*") is preserved as the wire name
    // only, via JsonPropertyName - the C# side uses correct spelling.
    [System.Text.Json.Serialization.JsonPropertyName("titel_name_en")]
    public string TitleEn { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("titel_name_ar")]
    public string TitleAr { get; set; } = string.Empty;

    // These hold the actual VALUE (e.g. "13.1", "220-240 V"), not a prose
    // description, despite the source's "description_name_*" key names.
    public string DescriptionNameEn { get; set; } = string.Empty;
    public string DescriptionNameAr { get; set; } = string.Empty;
}

public class MiddlewareProductInstallation
{
    public long InstallationId { get; set; }
    public long ProductId { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int IsSelected { get; set; } // 1/0 in JSON - converted to bool during upsert
}

public class MiddlewareOffer
{
    public long OfferId { get; set; }
    public long OfferGroupId { get; set; }
    public long ProductId { get; set; }
    public int Status { get; set; } // 1/0 in JSON - converted to bool during upsert
    public MiddlewareOfferGroup? OfferGroup { get; set; }
}

public class MiddlewareOfferGroup
{
    public long Id { get; set; }
    public int OrderId { get; set; }
    public string OfferNameEn { get; set; } = string.Empty;
    public string OfferNameAr { get; set; } = string.Empty;
    public string? Lastwordcolor { get; set; }
    public string? OfferIcon { get; set; }
    public string? OfferColor { get; set; }
    public bool IsBig { get; set; }
    public DateTimeOffset? ExpireAt { get; set; }
    public int Status { get; set; } // 1/0 in JSON - converted to bool during upsert
}