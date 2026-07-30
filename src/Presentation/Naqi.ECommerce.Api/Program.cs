// src/Presentation/Naqi.ECommerce.Api/Program.cs
//
// Same Infrastructure.AddInfrastructure() call as the Dashboard - same
// Users/Roles tables - but a JWT bearer scheme instead of cookies, since
// Next.js is a separate origin and can't rely on same-site cookies the
// way the server-rendered Dashboard can.

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Naqi.ECommerce.Api.Extensions;
using Naqi.ECommerce.Application;
using Naqi.ECommerce.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- Layers ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddNaqiLocalization(Path.Combine(AppContext.BaseDirectory, "wwwroot", "Resources"));

// ---- JWT bearer auth (Next.js sends Authorization: Bearer <token>) ----
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

// ---- CORS - Next.js runs on a different origin ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("NextJsStorefront", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // needed if you ever use httpOnly refresh-token cookies
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddNaqiSwagger(); // see Extensions/SwaggerServiceExtensions.cs

var app = builder.Build();
await app.MigrateNaqiDatabaseAsync();

app.UseNaqiSwagger(); // config-driven (Swagger:Enabled) - see SwaggerServiceExtensions

app.UseMiddleware<Naqi.ECommerce.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();

// RequestLocalizationOptions lives in Microsoft.AspNetCore.Localization,
// NOT Microsoft.AspNetCore.Builder - that mismatch was the earlier build error.
var localizationOptions = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseCors("NextJsStorefront");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();