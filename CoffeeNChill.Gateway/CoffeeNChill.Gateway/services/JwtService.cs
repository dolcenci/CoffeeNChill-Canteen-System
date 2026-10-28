using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoffeeNChill.Gateway.Models;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChill.Gateway.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AuthResponse GenerateToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? "CoffeeNChillSuperSecretKeyForJwtToken2026!@#Minimum32Chars";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "CoffeeNChill";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "CoffeeNChillClients";

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.RowKey),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.RowKey,
            FullName = user.FullName,
            Role = user.Role,
            ExpiresAt = expires
        };
    }
}