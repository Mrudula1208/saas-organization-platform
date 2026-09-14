using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.Application.DTOS.Auth;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.API.Controllers;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _auth = new();
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _controller = new AuthController(_auth.Object);
        }

        private static JsonElement AsJson(object? value)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement;
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorizedWithMessage()
        {
            _auth.Setup(x => x.LoginAsync(It.IsAny<LoginDto>())).ReturnsAsync((TokenResponseDto?)null);

            var result = await _controller.Login(new LoginDto { Email = "tenant@acme.com", Password = "wrong" });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var body = AsJson(unauthorized.Value);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("Invalid Email or Password", body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task Login_Success_ReturnsOkWithTokenData()
        {
            _auth.Setup(x => x.LoginAsync(It.IsAny<LoginDto>())).ReturnsAsync(new TokenResponseDto
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                Email = "tenant@acme.com",
                Role = "TenantAdmin",
                TenantId = Guid.NewGuid(),
                FullName = "Tenant Admin"
            });

            var result = await _controller.Login(new LoginDto { Email = "tenant@acme.com", Password = "tenant123" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = AsJson(ok.Value);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("tenant@acme.com", body.GetProperty("data").GetProperty("Email").GetString());
        }

        [Fact]
        public async Task Login_ServiceThrows_ReturnsBadRequestWithMessage()
        {
            _auth.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                .ThrowsAsync(new Exception("Database is down"));

            var result = await _controller.Login(new LoginDto { Email = "tenant@acme.com", Password = "tenant123" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Database is down", AsJson(badRequest.Value).GetProperty("message").GetString());
        }

        [Fact]
        public async Task RegisterTenant_DuplicateEmail_ReturnsBadRequestWithMessage()
        {
            _auth.Setup(x => x.RegisterTenantAsync(It.IsAny<RegisterTenantDto>()))
                .ThrowsAsync(new Exception("Email address is already in use."));

            var result = await _controller.RegisterTenant(new RegisterTenantDto { AdminEmail = "taken@acme.com" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Email address is already in use.", AsJson(badRequest.Value).GetProperty("message").GetString());
        }

        [Fact]
        public async Task RegisterTenant_Success_ReturnsOk()
        {
            _auth.Setup(x => x.RegisterTenantAsync(It.IsAny<RegisterTenantDto>()))
                .ReturnsAsync(new TokenResponseDto { AccessToken = "token", Email = "admin@newco.com" });

            var result = await _controller.RegisterTenant(new RegisterTenantDto { AdminEmail = "admin@newco.com" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(AsJson(ok.Value).GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task ForgotPassword_UnknownEmail_StillReturnsOkToPreventEnumeration()
        {
            // The endpoint must look the same whether or not the email exists.
            _auth.Setup(x => x.ForgotPasswordAsync(It.IsAny<ForgotPasswordDto>())).ReturnsAsync(false);

            var result = await _controller.ForgotPassword(new ForgotPasswordDto { Email = "nobody@acme.com" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(AsJson(ok.Value).GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
        {
            _auth.Setup(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordDto>())).ReturnsAsync(false);

            var result = await _controller.ResetPassword(new ResetPasswordDto());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(AsJson(badRequest.Value).GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task Logout_WithUserClaim_RevokesSessionForThatUser()
        {
            var userId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, userId: userId);

            var result = await _controller.Logout();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(AsJson(ok.Value).GetProperty("success").GetBoolean());
            _auth.Verify(x => x.LogoutAsync(userId), Times.Once);
        }
    }
}
