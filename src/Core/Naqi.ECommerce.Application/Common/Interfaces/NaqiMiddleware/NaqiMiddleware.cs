// src/Core/Naqi.ECommerce.Application/Common/Interfaces/NaqiMiddleware/MiddlewareProductDtos.cs
//
// Mirrors the JSON shape returned by NaqiEcommerceMiddleware's
// /api/v2/products endpoint. Only fields actually consumed by
// SyncProductsCommandHandler are kept strongly-typed here; anything else
// in the real response (variants, specifications, installations, offers,
// reviews...) is intentionally NOT modeled yet - add fields incrementally
// as the admin panel needs to surface them. Deserialization with
// System.Text.Json ignores unknown JSON properties by default, so this
// is safe even though the real payload has many more fields.

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
    public decimal ProductPrice { get; set; }
    public decimal onsaleprice { get; set; }
    public string? ProductMedia { get; set; } // comma-separated URLs, first one used as the thumbnail
    public int Quantity { get; set; }
    public MiddlewareProductQuantity? ProductQuantity { get; set; }
    public List<MiddlewareVariant>? Variants { get; set; }
    public List<MiddlewareUiCategory>? UiCategories { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
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
    public int ProductQnty { get; set; }
    public string? ProductMedia { get; set; }
}

public class MiddlewareUiCategory
{
    public int CategoryId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string? Image { get; set; }
}