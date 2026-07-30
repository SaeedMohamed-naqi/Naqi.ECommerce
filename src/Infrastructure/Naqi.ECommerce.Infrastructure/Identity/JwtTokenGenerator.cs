// src/Infrastructure/Naqi.ECommerce.Infrastructure/Identity/JwtTokenGenerator.cs
//
// Reads the same Jwt:Key/Jwt:Issuer/Jwt:Audience config keys the Api
// project's AddJwtBearer(...) setup validates against (see
// ApiProgram.cs) - signing and validation MUST agree on these values or
// every token this issues would fail validation.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Naqi.ECommerce.Application.Common.Interfaces;

namespace Naqi.ECommerce.Infrastructure.Identity;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration) => _configuration = configuration;

    public string GenerateToken(JwtTokenRequest request)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expiryMinutes = jwtSection.GetValue<int?>("ExpiryMinutes") ?? 60 * 24; // 24h default

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.Subject),
            new(ClaimTypes.Role, request.Role),
            new("customer_id", request.CustomerId.ToString()),
            new("is_guest", request.IsGuest ? "true" : "false"),
            new("phone_confirmed", request.PhoneConfirmed ? "true" : "false"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(request.Email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, request.Email));

        if (!string.IsNullOrWhiteSpace(request.Phone))
            claims.Add(new Claim(ClaimTypes.MobilePhone, request.Phone));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}