using AuthCore.Core.DTOs;
using AuthCore.Core.Entities;
using AuthCore.Core.Interfaces;
using AuthCore.Infrastructure.Data;
using AuthCore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AuthCore.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly AuthDbContext _context;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuthDbContext(options);
        _jwtServiceMock = new Mock<IJwtService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _cacheServiceMock = new Mock<ICacheService>();
        _configurationMock = new Mock<IConfiguration>();
        _emailServiceMock = new Mock<IEmailService>();

        _configurationMock.Setup(c => c["Jwt:RefreshTokenExpirationDays"]).Returns("7");

        _authService = new AuthService(
            _context,
            _jwtServiceMock.Object,
            _passwordHasherMock.Object,
            _cacheServiceMock.Object,
            _configurationMock.Object,
            _emailServiceMock.Object
        );

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var userRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "User",
            Description = "Standard user",
            CreatedAt = DateTime.UtcNow
        };

        _context.Roles.Add(userRole);
        _context.SaveChanges();
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ShouldCreateUser()
    {
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            TenantDomain = "testdomain"
        };

        _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns("hashed_password");
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .Returns("access_token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
            .Returns("refresh_token");

        var result = await _authService.RegisterAsync(request, "127.0.0.1");

        Assert.NotNull(result);
        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
        Assert.Equal(request.Email, result.User.Email);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        Assert.NotNull(user);
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ShouldThrowException()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Domain = "test",
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "existing@example.com",
            PasswordHash = "hash",
            FirstName = "Existing",
            LastName = "User",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            TenantDomain = "test"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _authService.RegisterAsync(request, "127.0.0.1"));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Domain = "test",
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "user@example.com",
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "Password123!",
            TenantDomain = "test"
        };

        _passwordHasherMock.Setup(p => p.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .Returns("access_token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
            .Returns("refresh_token");

        var result = await _authService.LoginAsync(request, "127.0.0.1");

        Assert.NotNull(result);
        Assert.Equal("access_token", result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ShouldThrowUnauthorizedException()
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Domain = "test",
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "user@example.com",
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword",
            TenantDomain = "test"
        };

        _passwordHasherMock.Setup(p => p.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _authService.LoginAsync(request, "127.0.0.1"));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
