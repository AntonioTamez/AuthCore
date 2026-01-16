using AuthCore.Core.DTOs;
using AuthCore.Core.Entities;
using AuthCore.Core.Interfaces;
using AuthCore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuthCore.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _configuration;

    public AuthService(
        AuthDbContext context,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        ICacheService cacheService,
        IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _cacheService = cacheService;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress)
    {
        var existingUser = await _context.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Domain == request.TenantDomain);
        
        if (tenant == null)
        {
            tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.TenantDomain,
                Domain = request.TenantDomain,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Tenants.Add(tenant);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (userRole != null)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = userRole.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
    {
        var query = _context.Users
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.TenantDomain))
        {
            query = query.Where(u => u.Email == request.Email && u.Tenant.Domain == request.TenantDomain);
        }
        else
        {
            query = query.Where(u => u.Email == request.Email);
        }

        var user = await query.FirstOrDefaultAsync();

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("User account is deactivated");

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Tenant)
            .Include(rt => rt.User.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null || !token.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;

        await _context.SaveChangesAsync();

        return await GenerateAuthResponseAsync(token.User, ipAddress);
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null || !token.IsActive)
            throw new InvalidOperationException("Invalid or expired refresh token");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;

        await _context.SaveChangesAsync();

        await _cacheService.RemoveAsync($"user_session:{token.UserId}");
    }

    public async Task<bool> RequestPasswordResetAsync(PasswordResetRequest request)
    {
        var query = _context.Users
            .Include(u => u.Tenant)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.TenantDomain))
        {
            query = query.Where(u => u.Email == request.Email && u.Tenant.Domain == request.TenantDomain);
        }
        else
        {
            query = query.Where(u => u.Email == request.Email);
        }

        var user = await query.FirstOrDefaultAsync();

        if (user == null)
            return false;

        user.PasswordResetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ResetPasswordAsync(PasswordResetConfirm request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

        if (user == null || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return false;

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, string ipAddress)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var accessToken = _jwtService.GenerateAccessToken(user, roles, permissions);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        var sessionData = new { UserId = user.Id, Roles = roles, Permissions = permissions };
        await _cacheService.SetAsync($"user_session:{user.Id}", sessionData, TimeSpan.FromMinutes(15));

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = refreshTokenEntity.ExpiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TenantDomain = user.Tenant.Domain,
                Roles = roles,
                Permissions = permissions
            }
        };
    }
}
