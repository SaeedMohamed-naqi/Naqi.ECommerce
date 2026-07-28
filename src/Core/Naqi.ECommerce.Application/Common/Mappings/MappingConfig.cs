// src/Core/Naqi.ECommerce.Application/Common/Mappings/MappingConfig.cs
//
// This is the Mapster equivalent of an AutoMapper Profile.
// Register all entity -> DTO (and DTO -> entity, where needed) rules here.

using Mapster;
//using Naqi.ECommerce.Domain.Entities;
//using Naqi.ECommerce.Application.Features.Products.DTOs;
//using Naqi.ECommerce.Application.Features.Orders.DTOs;

namespace Naqi.ECommerce.Application.Common.Mappings;

public class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        //// ---- Product -> ProductDto ----
        //// Simple 1:1 mapping - Mapster does this automatically by convention,
        //// but explicit config keeps it visible/documented and lets you
        //// customize flattening (e.g. Category.Name -> CategoryName).
        //config.NewConfig<Product, ProductDto>()
        //    .Map(dest => dest.CategoryName, src => src.Category.Name)
        //    .Map(dest => dest.InStock, src => src.StockQuantity > 0);

        //// ---- Order -> OrderDto (with nested items) ----
        //config.NewConfig<Order, OrderDto>()
        //    .Map(dest => dest.CustomerName, src => src.Customer.FullName)
        //    .Map(dest => dest.Items, src => src.OrderItems);

        //config.NewConfig<OrderItem, OrderItemDto>()
        //    .Map(dest => dest.ProductName, src => src.Product.Name);

        //// ---- Command -> Entity (creation mapping) ----
        //// Useful when a Command carries the same shape as the entity
        //config.NewConfig<Features.Products.Commands.CreateProductCommand, Product>()
        //    .Ignore(dest => dest.Id); // let EF/domain assign this
    }
}