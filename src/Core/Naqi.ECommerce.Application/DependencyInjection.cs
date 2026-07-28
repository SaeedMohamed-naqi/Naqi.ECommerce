// src/Core/Naqi.ECommerce.Application/DependencyInjection.cs
//
// Registers MediatR, FluentValidation, and Mapster for the Application layer.
// Called once from Api/Program.cs and Dashboard/Program.cs via:
//     builder.Services.AddApplication();

using System.Reflection;
using FluentValidation;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Naqi.ECommerce.Application.Common.Behaviors;
using Naqi.ECommerce.Application.Common.Mappings;

namespace Naqi.ECommerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR - scans this assembly for IRequestHandler<> implementations
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // FluentValidation - scans this assembly for AbstractValidator<> implementations
        services.AddValidatorsFromAssembly(assembly);

        // Pipeline behavior: runs validators automatically before every handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // ---- Mapster setup (replaces AutoMapper) ----
        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(assembly); // picks up MappingConfig : IRegister automatically

        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>(); // MapsterMapper.IMapper

        return services;
    }
}