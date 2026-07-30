// src/Core/Naqi.ECommerce.Application/Common/Mappings/MappingConfig.cs
//
// This is the Mapster equivalent of an AutoMapper Profile.
// Register all entity -> DTO (and DTO -> entity, where needed) rules here.

using Mapster;
 
using Naqi.ECommerce.Application.Features.Products.Queries;
using Naqi.ECommerce.Domain.Entities;

namespace Naqi.ECommerce.Application.Common.Mappings;

public class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {

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

        config.NewConfig<Naqi.ECommerce.Domain.Entities.ProductVariant,
            Naqi.ECommerce.Application.Features.Products.Queries.VariantDto>();

        // ProductOffer itself only has OfferGroupId + IsActive - the
        // display fields (name, icon, color...) live on the related
        // OfferGroup, so this mapping must flatten from that navigation
        // explicitly rather than relying on convention.
        config.NewConfig<Naqi.ECommerce.Domain.Entities.ProductOffer,
            Naqi.ECommerce.Application.Features.Products.Queries.OfferDto>()
            .Map(dest => dest.NameEn, src => src.OfferGroup.NameEn)
            .Map(dest => dest.NameAr, src => src.OfferGroup.NameAr)
            .Map(dest => dest.IconUrl, src => src.OfferGroup.IconUrl)
            .Map(dest => dest.Color, src => src.OfferGroup.Color)
            .Map(dest => dest.IsBig, src => src.OfferGroup.IsBig)
            .Map(dest => dest.ExpireAtUtc, src => src.OfferGroup.ExpireAtUtc)
            .Map(dest => dest.IsActive, src => src.IsActive);

        // ---- MiddlewareProduct -> ProductSyncData ----
        // Handles the fields that are a plain rename (ProductNameEn ->
        // NameEn, Subtagicon -> SubtagIconUrl, etc.) - everything else on
        // ProductSyncData is either genuinely COMPUTED (CategoryId is
        // resolved separately via a DB lookup; ImageUrl/AllImageUrls come
        // from splitting the comma-separated ProductMedia string;
        // StockQuantity is a three-way fallback chain; OldPrice/IsVertical/
        // Sku need conditional logic) rather than a field copy, so those
        // are explicitly Ignore()'d here - the handler patches them in via
        // a `with` expression after calling Adapt(), rather than pretending
        // Mapster can express that business logic declaratively.
        config.NewConfig<Naqi.ECommerce.Application.Common.Interfaces.NaqiMiddleware.MiddlewareProduct,
            Naqi.ECommerce.Domain.Entities.ProductSyncData>()
            .Map(dest => dest.NameEn, src => src.ProductNameEn)
            .Map(dest => dest.NameAr, src => src.ProductNameAr)
            .Map(dest => dest.ExternalProductId, src => src.ProductId)
            .Map(dest => dest.Price, src => src.OnSalePrice)
            .Map(dest => dest.TitleEn, src => src.ProductTitleEn)
            .Map(dest => dest.TitleAr, src => src.ProductTitleAr)
            .Map(dest => dest.DescriptionEn, src => src.ProductDescriptionEn)
            .Map(dest => dest.DescriptionAr, src => src.ProductDescriptionAr)
            .Map(dest => dest.TagEn, src => src.Tag)
            .Map(dest => dest.SubtagEn, src => src.Subtag)
            .Map(dest => dest.SubtagIconUrl, src => src.Subtagicon)
            // TagAr, SubtagAr, TagColor, RatingAverage, TotalRating,
            // WebsiteWarranty, WebsiteAccessories, WebsiteGuidelines,
            // WebsiteOtherSpecs all match by name already - no .Map() needed.
            .Ignore(dest => dest.Sku)
            .Ignore(dest => dest.OldPrice)
            .Ignore(dest => dest.StockQuantity)
            .Ignore(dest => dest.CategoryId)
            .Ignore(dest => dest.ImageUrl)
            .Ignore(dest => dest.AllImageUrls)
            .Ignore(dest => dest.IsVertical);

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

        // ---- Category -> CategoryDetailsDto ----
        // Flattens the Parent navigation (may be null for root categories)
        // into ParentNameEn/ParentNameAr. Subcategories and ProductCount
        // are populated separately by the query handler (they need extra
        // queries this entity's own data can't answer) - Ignore here to
        // be explicit that Mapster isn't expected to fill them.
        config.NewConfig<Category, Naqi.ECommerce.Application.Features.Categories.Queries.CategoryDetailsDto>()
            .Map(dest => dest.ParentNameEn, src => src.Parent != null ? src.Parent.NameEn : null)
            .Map(dest => dest.ParentNameAr, src => src.Parent != null ? src.Parent.NameAr : null)
            .Ignore(dest => dest.Subcategories)
            .Ignore(dest => dest.Products)
            .Ignore(dest => dest.ProductCount);

        config.NewConfig<CategoryBanner, Naqi.ECommerce.Application.Features.Categories.Queries.CategoryBannerDto>();

        // ---- OfferGroup -> OfferGroupDetailsDto ----
        // Own properties align 1:1 by name - Products/ProductCount are
        // populated separately by the query handler (from a ProductOffer
        // join, not from OfferGroup itself), so explicitly ignored here.
        config.NewConfig<OfferGroup, Naqi.ECommerce.Application.Features.Offers.Queries.OfferGroupDetailsDto>()
            .Ignore(dest => dest.Products)
            .Ignore(dest => dest.ProductCount);

      
    }
}