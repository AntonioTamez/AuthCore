using AuthCore.Core.DTOs;

namespace AuthCore.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress);
    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task RevokeTokenAsync(string refreshToken, string ipAddress);
    Task<bool> RequestPasswordResetAsync(PasswordResetRequest request);
    Task<bool> ResetPasswordAsync(PasswordResetConfirm request);
}
