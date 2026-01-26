namespace AuthCore.Core.DTOs;

public class OAuthLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // "Google" or "GitHub"
    public string TenantDomain { get; set; } = string.Empty;
}
