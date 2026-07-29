// src/Core/Naqi.ECommerce.Application/Common/Mappings/MappingConfig.cs
//
// This is the Mapster equivalent of an AutoMapper Profile.
// Register all entity -> DTO (and DTO -> entity, where needed) rules here.

using Mapster;
using Naqi.ECommerce.Domain.Entities;
//using Naqi.ECommerce.Application.Features.Products.DTOs;
//using Naqi.ECommerce.Application.Features.Orders.DTOs;

namespace Naqi.ECommerce.Application.Common.Mappings;

public class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // ---- Product -> ProductDto ----
        // Simple 1:1 mapping - Mapster does this automatically by convention,
        // but explicit config keeps it visible/documented and lets you
        // customize flattening (e.g. Category.Name -> CategoryName).
        //config.NewConfig<Product, ProductDto>()
        //    .Map(dest => dest.CategoryName, src => src.Category.Name)
        //    .Map(dest => dest.InStock, src => src.StockQuantity > 0);

        //// ---- Order -> OrderDto (with nested items) ----
        //config.NewConfig<Order, OrderDto>()
        //    .Map(dest => dest.CustomerName, src => src.Customer.FullName)
        //    .Map(dest => dest.Items, src => src.OrderItems);

        //config.NewConfig<OrderItem, OrderItemDto>()
            //.Map(dest => dest.ProductName, src => src.Product.Name);

        // ---- ProductSpecification -> SpecificationDto, ProductCategory -> UiCategoryDto ----
        // Property names already align 1:1, so Mapster's convention would
        // likely handle these automatically - registered explicitly anyway
        // so the collection-of-entity -> collection-of-DTO conversion used
        // inside Product -> ProductDetailsDto is guaranteed to resolve,
        // rather than depending on Mapster generating it on the fly.
        config.NewConfig<Naqi.ECommerce.Domain.Entities.ProductSpecification,
            Naqi.ECommerce.Application.Features.Products.Queries.SpecificationDto>();

        config.NewConfig<Naqi.ECommerce.Domain.Entities.ProductCategory,
            Naqi.ECommerce.Application.Features.Products.Queries.UiCategoryDto>();

        config.NewConfig<Naqi.ECommerce.Domain.Entities.ProductInstallation,
            Naqi.ECommerce.Application.Features.Products.Queries.InstallationDto>();

        // ---- Product -> ProductDetailsDto ----
        // Explicit even though Mapster's naming convention would likely
        // flatten Category.NameEn -> CategoryNameEn automatically anyway -
        // being explicit here means it's documented and won't silently
        // break if the DTO's property names ever drift from that convention.
        // AllImageUrls is deliberately NOT mapped here (Ignore) - it needs
        // a string-split transformation, which the query handler applies
        // manually after calling Adapt().
        config.NewConfig<Product, Naqi.ECommerce.Application.Features.Products.Queries.ProductDetailsDto>()
            .Map(dest => dest.CategoryNameEn, src => src.Category.NameEn)
            .Map(dest => dest.CategoryNameAr, src => src.Category.NameAr)
            .Ignore(dest => dest.AllImageUrls);

        //// ---- Command -> Entity (creation mapping) ----
        //// Useful when a Command carries the same shape as the entity
        //config.NewConfig<Features.Products.Commands.CreateProductCommand, Product>()
        //    .Ignore(dest => dest.Id); // let EF/domain assign this
    }
}