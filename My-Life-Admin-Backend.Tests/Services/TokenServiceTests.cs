using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using My_Life_Admin.Models;
using My_Life_Admin.Options;
using My_Life_Admin.Services;

namespace MyLife_Admin.Tests.Services;

public class TokenServiceTests
{
    private static readonly JwtOptions JwtOptions = new()
    {
        Issuer = "MyLifeAdmin.Api",
        Audience = "MyLifeAdmin.Frontend",
        ExpiresMinutes = 30,
        SigningKey = "super-secret-signing-key-please-change"
    };

    private readonly TokenService _service = new(Options.Create(JwtOptions));

    [Fact]
    public void CreateToken_ValidUser_ReturnsJwtWithExpectedClaimsAndLifetime()
    {
        var user = new User { Id = 42, Name = "Anna", Email = "anna@example.com" };
        var before = DateTime.UtcNow;

        var tokenString = _service.CreateToken(user);
        var after = DateTime.UtcNow;

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        Assert.Equal(JwtOptions.Issuer, token.Issuer);
        Assert.Contains(JwtOptions.Audience, token.Audiences);
        Assert.Equal("42", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("42", token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("Anna", token.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("anna@example.com", token.Claims.First(c => c.Type == ClaimTypes.Email).Value);

        var expectedExpiryStart = before.AddMinutes(JwtOptions.ExpiresMinutes).AddSeconds(-1);
        var expectedExpiryEnd = after.AddMinutes(JwtOptions.ExpiresMinutes).AddSeconds(1);
        Assert.InRange(token.ValidTo, expectedExpiryStart, expectedExpiryEnd);
    }

    [Fact]
    public void CreateToken_ValidUser_ProducesTokenThatPassesValidation()
    {
        var user = new User { Id = 7, Name = "Elsa", Email = "elsa@example.com" };
        var tokenString = _service.CreateToken(user);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = JwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = JwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtOptions.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(tokenString, parameters, out _);

        Assert.Equal("7", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void CreateToken_NullUser_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.CreateToken(null!));
    }

    [Theory]
    [InlineData(0, "Anna", "anna@example.com", "User ID must be greater than zero.")]
    [InlineData(1, "", "anna@example.com", "User name is required.")]
    [InlineData(1, "Anna", "", "User email is required.")]
    public void CreateToken_InvalidUser_ThrowsArgumentException(
        int id,
        string name,
        string email,
        string expectedMessageStart)
    {
        var user = new User { Id = id, Name = name, Email = email };

        var ex = Assert.Throws<ArgumentException>(() => _service.CreateToken(user));

        Assert.StartsWith(expectedMessageStart, ex.Message);
    }
}
