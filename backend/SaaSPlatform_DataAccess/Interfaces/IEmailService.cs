using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null);
        Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string? clientAppUrl = null);
        Task SendUserInvitationEmailAsync(string toEmail, string recipientName, string tenantName, string role, string temporaryPassword, string? clientAppUrl = null);
        Task SendWelcomeEmailAsync(string toEmail, string recipientName, string tenantName, string? clientAppUrl = null);
    }
}
