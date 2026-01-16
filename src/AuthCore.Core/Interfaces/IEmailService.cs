namespace AuthCore.Core.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, string tenantDomain);
    Task SendEmailAsync(string to, string subject, string body);
}
