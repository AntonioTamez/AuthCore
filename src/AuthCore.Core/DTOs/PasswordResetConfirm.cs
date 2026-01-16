namespace AuthCore.Core.DTOs;

public class PasswordResetConfirm
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
