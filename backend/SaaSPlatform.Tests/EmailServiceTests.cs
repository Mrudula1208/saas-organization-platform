using Microsoft.Extensions.Options;
using Moq;
using SaaSPlatform.Application.DTOS.Email;
using SaaSPlatform.Application.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class EmailServiceTests
    {
        [Fact]
        public async Task SendEmailAsync_EmptyRecipient_ThrowsArgumentException()
        {
            var options = Options.Create(new EmailSettings());
            var service = new EmailService(options);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.SendEmailAsync("", "Subject", "Body"));
        }

        [Fact]
        public async Task SendEmailAsync_UnconfiguredSmtp_CompletesGracefullyWithoutThrowing()
        {
            var options = Options.Create(new EmailSettings
            {
                SmtpHost = "" // unconfigured/simulation mode
            });
            var service = new EmailService(options);

            var exception = await Record.ExceptionAsync(() =>
                service.SendEmailAsync("user@example.com", "Welcome", "<p>Welcome</p>", "Welcome"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task SendPasswordResetEmailAsync_GeneratesLinkAndCompletes()
        {
            var options = Options.Create(new EmailSettings
            {
                ClientAppUrl = "http://localhost:4200"
            });
            var service = new EmailService(options);

            var exception = await Record.ExceptionAsync(() =>
                service.SendPasswordResetEmailAsync("reset@test.com", "sample-reset-token-1234"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task SendUserInvitationEmailAsync_CompletesSuccessfully()
        {
            var options = Options.Create(new EmailSettings
            {
                ClientAppUrl = "http://localhost:4200"
            });
            var service = new EmailService(options);

            var exception = await Record.ExceptionAsync(() =>
                service.SendUserInvitationEmailAsync("member@acme.com", "Jane Doe", "Acme Corp", "Member", "tempPassword123"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task SendWelcomeEmailAsync_CompletesSuccessfully()
        {
            var options = Options.Create(new EmailSettings());
            var service = new EmailService(options);

            var exception = await Record.ExceptionAsync(() =>
                service.SendWelcomeEmailAsync("admin@acme.com", "John Admin", "Acme Corp"));

            Assert.Null(exception);
        }
    }
}
