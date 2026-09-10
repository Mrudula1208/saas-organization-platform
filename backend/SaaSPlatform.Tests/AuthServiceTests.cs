using Microsoft.Extensions.Configuration;
using Moq;
using SaaSPlatform.Application.DTOS.Auth;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model;
using SaaSPlatform_Model.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<ISystemLogRepository> _logs = new();
        private readonly Mock<ISubscriptionPlanRepository> _plans = new();
        private readonly Mock<ITenantRepository> _tenants = new();
        private readonly Mock<IPlatformSettingsRepository> _settings = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _settings.Setup(x => x.GetSettingsAsync())
                .ReturnsAsync(new PlatformSetting
                {
                    Id = Guid.NewGuid(),
                    PlatformName = "SaaS Platform",
                    MaintenanceMode = false,
                    AllowRegistrations = true
                });

            _unitOfWork.Setup(x => x.Users).Returns(_users.Object);
            _unitOfWork.Setup(x => x.SystemLogs).Returns(_logs.Object);
            _unitOfWork.Setup(x => x.SubscriptionPlans).Returns(_plans.Object);
            _unitOfWork.Setup(x => x.Tenants).Returns(_tenants.Object);
            _unitOfWork.Setup(x => x.Settings).Returns(_settings.Object);
            _unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

            _service = new AuthService(_unitOfWork.Object, CreateConfig(), null!);
        }

        // AuthService does not use AutoMapper, so no mapper is passed.
        private static IConfiguration CreateConfig()
        {
            return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "unit-test-signing-key-that-is-at-least-32-chars",
                ["Jwt:Issuer"] = "SaaSPlatform.Tests",
                ["Jwt:Audience"] = "SaaSPlatform.Tests"
            }).Build();
        }

        private static User CreateActiveUser(string email, string password)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                FullName = "Test User",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "TenantAdmin",
                TenantId = Guid.NewGuid(),
                IsActive = true
            };
        }

        [Fact]
        public async Task LoginAsync_UnknownEmail_ReturnsNullAndAuditsFailedAttempt()
        {
            _users.Setup(x => x.GetByEmailAsync("missing@acme.com")).ReturnsAsync((User?)null);

            var result = await _service.LoginAsync(new LoginDto { Email = "missing@acme.com", Password = "whatever" });

            Assert.Null(result);
            _logs.Verify(
                x => x.LogAsync("LOGIN_FAILED", It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsNullIncrementsAttemptsAndNeverLogsPassword()
        {
            const string submittedPassword = "wrong-password-123";
            var user = CreateActiveUser("tenant@acme.com", "correct-password");
            _users.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);

            var result = await _service.LoginAsync(new LoginDto { Email = user.Email, Password = submittedPassword });

            Assert.Null(result);
            Assert.Equal(1, user.FailedLoginAttempts);

            // The audit entry must exist but must never contain the submitted password.
            _logs.Verify(
                x => x.LogAsync(
                    "LOGIN_FAILED",
                    It.Is<string>(message => !message.Contains(submittedPassword)),
                    user.Id,
                    user.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_FifthFailedAttempt_LocksAccount()
        {
            var user = CreateActiveUser("member@acme.com", "correct-password");
            user.FailedLoginAttempts = 4;
            _users.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);

            var result = await _service.LoginAsync(new LoginDto { Email = user.Email, Password = "bad" });

            Assert.Null(result);
            Assert.Equal(5, user.FailedLoginAttempts);
            Assert.NotNull(user.LockoutEnd);
            Assert.True(user.LockoutEnd > DateTime.UtcNow.AddMinutes(14));
            _logs.Verify(
                x => x.LogAsync("ACCOUNT_LOCKOUT", It.IsAny<string>(), user.Id, user.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_LockedAccount_ThrowsEvenWithCorrectPassword()
        {
            var user = CreateActiveUser("member@acme.com", "correct-password");
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
            _users.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.LoginAsync(new LoginDto { Email = user.Email, Password = "correct-password" }));

            Assert.Contains("Account is locked", ex.Message);
            _users.Verify(x => x.UpdateUser(It.IsAny<Guid>(), It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokenAndResetsLockoutState()
        {
            var user = CreateActiveUser("tenant@acme.com", "correct-password");
            user.FailedLoginAttempts = 3;
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(-1); // expired lockout
            _users.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);

            var result = await _service.LoginAsync(new LoginDto { Email = user.Email, Password = "correct-password" });

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result!.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.Role, result.Role);
            Assert.Equal(user.TenantId, result.TenantId);

            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LockoutEnd);
            _logs.Verify(
                x => x.LogAsync("LOGIN_SUCCESS", It.IsAny<string>(), user.Id, user.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task RegisterTenantAsync_DuplicateEmail_Throws()
        {
            var existing = CreateActiveUser("taken@acme.com", "password");
            _users.Setup(x => x.GetByEmailAsync(existing.Email)).ReturnsAsync(existing);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.RegisterTenantAsync(new RegisterTenantDto
                {
                    Name = "New Co",
                    Domain = "newco.com",
                    AdminName = "Admin",
                    AdminEmail = existing.Email,
                    Password = "secret123",
                    ConfirmPassword = "secret123",
                    Plan = "Basic"
                }));

            Assert.Equal("Email address is already in use.", ex.Message);
        }

        [Fact]
        public async Task RegisterTenantAsync_UnknownPlan_Throws()
        {
            _users.Setup(x => x.GetByEmailAsync("admin@newco.com")).ReturnsAsync((User?)null);
            _plans.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<SubscriptionPlan> { new() { Id = Guid.NewGuid(), Name = "Basic" } });

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.RegisterTenantAsync(new RegisterTenantDto
                {
                    Name = "New Co",
                    Domain = "newco.com",
                    AdminName = "Admin",
                    AdminEmail = "admin@newco.com",
                    Password = "secret123",
                    ConfirmPassword = "secret123",
                    Plan = "Enterprise"
                }));

            Assert.Contains("was not found", ex.Message);
        }

        [Fact]
        public async Task RegisterTenantAsync_ValidInput_CreatesTenantAdminWithHashedPassword()
        {
            var planId = Guid.NewGuid();
            _users.Setup(x => x.GetByEmailAsync("admin@newco.com")).ReturnsAsync((User?)null);
            _plans.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<SubscriptionPlan> { new() { Id = planId, Name = "Basic" } });

            Tenant? addedTenant = null;
            _tenants.Setup(x => x.AddAsync(It.IsAny<Tenant>()))
                .Callback<Tenant>(t => addedTenant = t)
                .ReturnsAsync((Tenant t) => t);

            User? createdUser = null;
            _users.Setup(x => x.CreateUser(It.IsAny<User>()))
                .Callback<User>(u => createdUser = u)
                .ReturnsAsync((User u) => u);
            _users.Setup(x => x.UpdateUser(It.IsAny<Guid>(), It.IsAny<User>())).ReturnsAsync(true);

            // Plan name matching is case insensitive on purpose.
            var result = await _service.RegisterTenantAsync(new RegisterTenantDto
            {
                Name = "New Co",
                Domain = "newco.com",
                AdminName = "Jane Admin",
                AdminEmail = "admin@newco.com",
                Password = "secret123",
                ConfirmPassword = "secret123",
                Plan = "BASIC"
            });

            Assert.NotNull(result);
            Assert.NotNull(addedTenant);
            Assert.NotNull(createdUser);
            Assert.Equal(planId, addedTenant!.SubscriptionPlanId);
            Assert.Equal("admin@newco.com", addedTenant.ContactEmail);
            Assert.True(addedTenant.IsActive);

            Assert.Equal(addedTenant.Id, createdUser!.TenantId);
            Assert.Equal("TenantAdmin", createdUser.Role);
            // The plain text password is stored only as a BCrypt hash.
            Assert.True(BCrypt.Net.BCrypt.Verify("secret123", createdUser.PasswordHash));

            _logs.Verify(x => x.LogAsync("TENANT_CREATED", It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
            _logs.Verify(x => x.LogAsync("USER_REGISTERED", It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_WrongToken_ReturnsFalse()
        {
            var user = CreateActiveUser("tenant@acme.com", "old-password");
            user.PasswordResetToken = "real-token";
            user.ResetTokenExpiryTime = DateTime.UtcNow.AddHours(2);
            _users.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);

            var result = await _service.ResetPasswordAsync(new ResetPasswordDto
            {
                Email = user.Email,
                Token = "wrong-token",
                Password = "new-password",
                ConfirmPassword = "new-password"
            });

            Assert.False(result);
            _users.Verify(x => x.UpdateUser(It.IsAny<Guid>(), It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_ExpiredToken_ReturnsFalse()
        {
            var user = CreateActiveUser("tenant@acme.com", "old-password");
            user.PasswordResetToken = "real-token";
            user.ResetTokenExpiryTime = DateTime.UtcNow.AddHours(-1);
            _users.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);

            var result = await _service.ResetPasswordAsync(new ResetPasswordDto
            {
                Email = user.Email,
                Token = "real-token",
                Password = "new-password",
                ConfirmPassword = "new-password"
            });

            Assert.False(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordAndClearsLockout()
        {
            var user = CreateActiveUser("tenant@acme.com", "old-password");
            user.PasswordResetToken = "real-token";
            user.ResetTokenExpiryTime = DateTime.UtcNow.AddHours(2);
            user.FailedLoginAttempts = 5;
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
            _users.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);

            var result = await _service.ResetPasswordAsync(new ResetPasswordDto
            {
                Email = user.Email,
                Token = "real-token",
                Password = "brand-new-password",
                ConfirmPassword = "brand-new-password"
            });

            Assert.True(result);
            Assert.True(BCrypt.Net.BCrypt.Verify("brand-new-password", user.PasswordHash));
            Assert.False(BCrypt.Net.BCrypt.Verify("old-password", user.PasswordHash));
            Assert.Null(user.PasswordResetToken);
            Assert.Null(user.ResetTokenExpiryTime);
            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LockoutEnd);
            _logs.Verify(
                x => x.LogAsync("PASSWORD_RESET", It.IsAny<string>(), user.Id, user.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UnknownEmail_ReturnsFalse()
        {
            _users.Setup(x => x.GetByEmailAsync("nobody@acme.com")).ReturnsAsync((User?)null);

            var result = await _service.ForgotPasswordAsync(new ForgotPasswordDto { Email = "nobody@acme.com" });

            Assert.False(result);
        }

        [Fact]
        public async Task ForgotPasswordAsync_KnownEmail_SetsResetTokenWithTwoHourExpiry()
        {
            var user = CreateActiveUser("tenant@acme.com", "password");
            _users.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);

            var result = await _service.ForgotPasswordAsync(new ForgotPasswordDto { Email = user.Email });

            Assert.True(result);
            Assert.False(string.IsNullOrWhiteSpace(user.PasswordResetToken));
            Assert.True(user.ResetTokenExpiryTime > DateTime.UtcNow.AddHours(1.9));
        }

        [Fact]
        public async Task LogoutAsync_RevokesRefreshTokenAndAudits()
        {
            var user = CreateActiveUser("tenant@acme.com", "password");
            user.RefreshToken = "refresh-token";
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            _users.Setup(x => x.GetUserById(user.Id)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);

            await _service.LogoutAsync(user.Id);

            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);
            _logs.Verify(
                x => x.LogAsync("LOGOUT", It.IsAny<string>(), user.Id, user.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task LoginAsync_DuringMaintenanceMode_ThrowsForNonSuperAdmin()
        {
            var user = CreateActiveUser("tenant@acme.com", "correct-password");
            user.Role = "TenantAdmin";
            _users.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _settings.Setup(x => x.GetSettingsAsync())
                .ReturnsAsync(new PlatformSetting { MaintenanceMode = true, AllowRegistrations = true });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.LoginAsync(new LoginDto { Email = user.Email, Password = "correct-password" }));

            Assert.Contains("maintenance", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoginAsync_DuringMaintenanceMode_AllowsSuperAdmin()
        {
            var user = CreateActiveUser("admin@system.com", "correct-password");
            user.Role = "SuperAdmin";
            _users.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);
            _settings.Setup(x => x.GetSettingsAsync())
                .ReturnsAsync(new PlatformSetting { MaintenanceMode = true, AllowRegistrations = true });

            var result = await _service.LoginAsync(new LoginDto { Email = user.Email, Password = "correct-password" });

            Assert.NotNull(result);
            Assert.Equal("SuperAdmin", result!.Role);
        }

        [Fact]
        public async Task RegisterTenantAsync_WhenMaintenanceMode_Throws()
        {
            _settings.Setup(x => x.GetSettingsAsync())
                .ReturnsAsync(new PlatformSetting { MaintenanceMode = true, AllowRegistrations = true });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RegisterTenantAsync(new RegisterTenantDto
                {
                    Name = "New Co",
                    Domain = "newco.com",
                    AdminName = "Admin",
                    AdminEmail = "admin@newco.com",
                    Password = "secret123",
                    ConfirmPassword = "secret123",
                    Plan = "Basic"
                }));

            Assert.Contains("maintenance", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RegisterTenantAsync_WhenRegistrationsDisabled_Throws()
        {
            _settings.Setup(x => x.GetSettingsAsync())
                .ReturnsAsync(new PlatformSetting { MaintenanceMode = false, AllowRegistrations = false });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RegisterTenantAsync(new RegisterTenantDto
                {
                    Name = "New Co",
                    Domain = "newco.com",
                    AdminName = "Admin",
                    AdminEmail = "admin@newco.com",
                    Password = "secret123",
                    ConfirmPassword = "secret123",
                    Plan = "Basic"
                }));

            Assert.Contains("disabled", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
