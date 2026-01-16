namespace AuthCore.Core.DTOs;

public class PasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
    public string? TenantDomain { get; set; }
}
