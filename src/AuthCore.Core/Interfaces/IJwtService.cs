using AuthCore.Core.Entities;
using System.Security.Claims;

namespace AuthCore.Core.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, List<string> roles, List<string> permissions);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
