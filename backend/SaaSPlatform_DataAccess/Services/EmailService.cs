using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaaSPlatform.Application.DTOS.Email;
using SaaSPlatform.Application.Interfaces;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService>? _logger;

        public EmailService(IOptions<EmailSettings> options, ILogger<EmailService>? logger = null)
        {
            _settings = options?.Value ?? new EmailSettings();
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException("Recipient email address is required.", nameof(toEmail));
            }

            // If SMTP is not configured or uses a placeholder host, simulate gracefully without breaking
            if (string.IsNullOrWhiteSpace(_settings.SmtpHost) || _settings.SmtpHost.Contains("example.com"))
            {
                _logger?.LogInformation(
                    "[EMAIL SIMULATION] To: {To} | Subject: {Subject}\nBody Preview: {Preview}",
                    toEmail, subject, textBody ?? "HTML content dispatched.");
                Console.WriteLine($"[EMAIL SIMULATION] Sent to: {toEmail} | Subject: {subject}");
                return;
            }

            try
            {
                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrWhiteSpace(_settings.UserName))
                {
                    client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
                }

                using var mail = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);

                if (!string.IsNullOrWhiteSpace(textBody))
                {
                    var plainTextView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
                    mail.AlternateViews.Add(plainTextView);
                }

                await client.SendMailAsync(mail);
                _logger?.LogInformation("Email successfully dispatched to {To} with subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send email to {To} via SMTP host {Host}", toEmail, _settings.SmtpHost);
                // In production, log and rethrow or fallback
                throw new InvalidOperationException($"Failed to deliver email: {ex.Message}", ex);
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string? clientAppUrl = null)
        {
            var baseUrl = (clientAppUrl ?? _settings.ClientAppUrl).TrimEnd('/');
            var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(toEmail)}&token={Uri.EscapeDataString(resetToken)}";
            var subject = "Reset Your Password - SaaS Platform";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 0; color: #1e293b; }}
    .container {{ max-width: 560px; margin: 40px auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); }}
    .header {{ background: #4f46e5; padding: 28px; text-align: center; color: #ffffff; }}
    .header h1 {{ margin: 0; font-size: 22px; font-weight: 700; letter-spacing: -0.02em; }}
    .content {{ padding: 32px 28px; }}
    .content p {{ font-size: 15px; line-height: 1.6; margin: 0 0 16px 0; color: #334155; }}
    .btn-container {{ text-align: center; margin: 28px 0; }}
    .btn {{ display: inline-block; background: #4f46e5; color: #ffffff !important; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 15px; }}
    .note {{ font-size: 13px; color: #64748b; line-height: 1.5; margin-top: 24px; padding-top: 20px; border-top: 1px solid #f1f5f9; }}
    .link-alt {{ word-break: break-all; color: #4f46e5; font-size: 13px; }}
    .footer {{ background: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>Password Reset Request</h1>
    </div>
    <div class='content'>
      <p>Hello,</p>
      <p>We received a request to reset your password for your SaaS Organization Platform account. Click the button below to choose a new password:</p>
      <div class='btn-container'>
        <a href='{resetLink}' class='btn' target='_blank'>Reset My Password</a>
      </div>
      <p class='note'>
        <strong>Note:</strong> This password reset link will expire in <strong>2 hours</strong>.<br/>
        If you did not request a password reset, you can safely ignore this email. Your password will remain unchanged.
      </p>
      <p class='note'>
        If the button above does not work, copy and paste this URL into your browser:<br/>
        <a href='{resetLink}' class='link-alt'>{resetLink}</a>
      </p>
    </div>
    <div class='footer'>
      &copy; {DateTime.UtcNow.Year} SaaS Organization Platform. All rights reserved.
    </div>
  </div>
</body>
</html>";

            var text = $@"Password Reset Request

Hello,

We received a request to reset your password. Use the following link to set a new password (valid for 2 hours):

{resetLink}

If you did not request this, please ignore this email.

SaaS Organization Platform";

            Console.WriteLine($"[EMAIL SIMULATION] Reset Password Link for {toEmail}: {resetLink}");
            await SendEmailAsync(toEmail, subject, html, text);
        }

        public async Task SendUserInvitationEmailAsync(
            string toEmail,
            string recipientName,
            string tenantName,
            string role,
            string temporaryPassword,
            string? clientAppUrl = null)
        {
            var baseUrl = (clientAppUrl ?? _settings.ClientAppUrl).TrimEnd('/');
            var loginLink = $"{baseUrl}/login";
            var subject = $"You've been invited to join {tenantName} on SaaS Platform";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 0; color: #1e293b; }}
    .container {{ max-width: 560px; margin: 40px auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); }}
    .header {{ background: #4f46e5; padding: 28px; text-align: center; color: #ffffff; }}
    .header h1 {{ margin: 0; font-size: 22px; font-weight: 700; }}
    .content {{ padding: 32px 28px; }}
    .content p {{ font-size: 15px; line-height: 1.6; margin: 0 0 16px 0; color: #334155; }}
    .credentials-box {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; margin: 20px 0; font-size: 14px; }}
    .btn-container {{ text-align: center; margin: 28px 0; }}
    .btn {{ display: inline-block; background: #4f46e5; color: #ffffff !important; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 15px; }}
    .footer {{ background: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>Welcome to {tenantName}</h1>
    </div>
    <div class='content'>
      <p>Hello {recipientName},</p>
      <p>You have been invited to join the <strong>{tenantName}</strong> organization workspace on the SaaS Platform as a <strong>{role}</strong>.</p>
      
      <div class='credentials-box'>
        <strong>Your Login Credentials:</strong><br/>
        Email: <code>{toEmail}</code><br/>
        Temporary Password: <code>{temporaryPassword}</code>
      </div>

      <div class='btn-container'>
        <a href='{loginLink}' class='btn' target='_blank'>Sign In to Your Workspace</a>
      </div>

      <p style='font-size: 13px; color: #64748b;'>We recommend changing your password after signing in to your profile settings.</p>
    </div>
    <div class='footer'>
      &copy; {DateTime.UtcNow.Year} SaaS Organization Platform. All rights reserved.
    </div>
  </div>
</body>
</html>";

            var text = $@"Welcome to {tenantName}!

Hello {recipientName},

You have been invited to join the {tenantName} organization workspace on SaaS Platform as a {role}.

Your Login Credentials:
Email: {toEmail}
Temporary Password: {temporaryPassword}

Sign in here: {loginLink}

SaaS Organization Platform";

            Console.WriteLine($"[EMAIL SIMULATION] User Invitation for {toEmail} in {tenantName}. Password: {temporaryPassword}");
            await SendEmailAsync(toEmail, subject, html, text);
        }

        public async Task SendWelcomeEmailAsync(
            string toEmail,
            string recipientName,
            string tenantName,
            string? clientAppUrl = null)
        {
            var baseUrl = (clientAppUrl ?? _settings.ClientAppUrl).TrimEnd('/');
            var loginLink = $"{baseUrl}/login";
            var subject = $"Welcome to SaaS Platform - {tenantName} Workspace Created!";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 0; color: #1e293b; }}
    .container {{ max-width: 560px; margin: 40px auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; }}
    .header {{ background: #4f46e5; padding: 28px; text-align: center; color: #ffffff; }}
    .header h1 {{ margin: 0; font-size: 22px; font-weight: 700; }}
    .content {{ padding: 32px 28px; }}
    .content p {{ font-size: 15px; line-height: 1.6; margin: 0 0 16px 0; color: #334155; }}
    .btn-container {{ text-align: center; margin: 28px 0; }}
    .btn {{ display: inline-block; background: #4f46e5; color: #ffffff !important; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 15px; }}
    .footer {{ background: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>Your Workspace Is Ready!</h1>
    </div>
    <div class='content'>
      <p>Hello {recipientName},</p>
      <p>Congratulations! Your workspace for <strong>{tenantName}</strong> has been successfully registered on the SaaS Organization Platform.</p>
      <p>You can now manage team members, launch projects, track tasks, and configure your tenant settings.</p>
      
      <div class='btn-container'>
        <a href='{loginLink}' class='btn' target='_blank'>Go to Dashboard</a>
      </div>
    </div>
    <div class='footer'>
      &copy; {DateTime.UtcNow.Year} SaaS Organization Platform. All rights reserved.
    </div>
  </div>
</body>
</html>";

            var text = $@"Welcome to SaaS Platform!

Hello {recipientName},

Your workspace for {tenantName} is ready. Sign in to get started:
{loginLink}

SaaS Organization Platform";

            Console.WriteLine($"[EMAIL SIMULATION] Welcome Email for {toEmail} ({tenantName}).");
            await SendEmailAsync(toEmail, subject, html, text);
        }
    }
}
