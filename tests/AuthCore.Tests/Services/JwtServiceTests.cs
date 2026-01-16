using AuthCore.Core.Entities;
using AuthCore.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using Xunit;

namespace AuthCore.Tests.Services;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;

    public JwtServiceTests()
    {
        var configurationData = new Dictionary<string, string>
        {
            { "Jwt:Secret", "ThisIsATestSecretKeyThatIsAtLeast32CharactersLongForHS256" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:AccessTokenExpirationMinutes", "15" },
            { "Jwt:RefreshTokenExpirationDays", "7" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        _jwtService = new JwtService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            TenantId = Guid.NewGuid()
        };
        var roles = new List<string> { "User" };
        var permissions = new List<string> { "users.read" };

        var token = _jwtService.GenerateAccessToken(user, roles, permissions);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        var token = _jwtService.GenerateRefreshToken();

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GenerateRefreshToken_MultipleInvocations_ShouldProduceDifferentTokens()
    {
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void ValidateToken_ValidToken_ShouldReturnClaimsPrincipal()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            TenantId = Guid.NewGuid()
        };
        var roles = new List<string> { "User" };
        var permissions = new List<string> { "users.read" };

        var token = _jwtService.GenerateAccessToken(user, roles, permissions);
        var principal = _jwtService.ValidateToken(token);

        Assert.NotNull(principal);
        Assert.Equal(user.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(user.Email, principal.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ShouldReturnNull()
    {
        var invalidToken = "invalid.token.here";
        var principal = _jwtService.ValidateToken(invalidToken);

        Assert.Null(principal);
    }
}
