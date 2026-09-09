using Moq;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Users;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform_Model;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<ISystemLogRepository> _logs = new();
        private readonly UserService _service;

        public UserServiceTests()
        {
            _service = new UserService(_users.Object, _logs.Object);
        }

        private static User CreateStoredUser(string password)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                FullName = "Jane Member",
                Email = "jane@acme.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "Member",
                TenantId = Guid.NewGuid(),
                IsActive = true
            };
        }

        [Fact]
        public async Task CreateUser_PlaintextPassword_IsHashedBeforeStorage()
        {
            _users.Setup(x => x.CreateUser(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);

            var created = await _service.CreateUser(new User
            {
                FullName = "John Member",
                Email = "john@acme.com",
                PasswordHash = "plain-text-password",
                Role = "Member",
                TenantId = Guid.NewGuid()
            });

            Assert.NotEqual("plain-text-password", created.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("plain-text-password", created.PasswordHash));
            Assert.True(created.IsActive);
            Assert.False(created.IsDeleted);
            _logs.Verify(
                x => x.LogAsync("USER_CREATED", It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateUser_AlreadyHashedPassword_IsNotHashedTwice()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("already-hashed");
            _users.Setup(x => x.CreateUser(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);

            var created = await _service.CreateUser(new User
            {
                FullName = "John Member",
                Email = "john@acme.com",
                PasswordHash = hash,
                Role = "Member",
                TenantId = Guid.NewGuid()
            });

            // Hashing twice would break login, so the original hash must be kept as-is.
            Assert.Equal(hash, created.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("already-hashed", created.PasswordHash));
        }

        [Fact]
        public async Task ChangePasswordAsync_ConfirmationMismatch_Throws()
        {
            var dto = new ChangePasswordDto
            {
                CurrentPassword = "current-1",
                NewPassword = "new-password-1",
                ConfirmPassword = "different-confirmation"
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ChangePasswordAsync(Guid.NewGuid(), dto));

            Assert.Equal("New password and confirmation do not match.", ex.Message);
            // The check happens before anything is loaded or saved.
            _users.Verify(x => x.GetUserById(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrentPassword_Throws()
        {
            var user = CreateStoredUser("correct-current-password");
            _users.Setup(x => x.GetUserById(user.Id)).ReturnsAsync(user);

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "wrong-current-password",
                NewPassword = "brand-new-password",
                ConfirmPassword = "brand-new-password"
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ChangePasswordAsync(user.Id, dto));

            Assert.Equal("Current password is incorrect.", ex.Message);
            _users.Verify(x => x.UpdateUser(It.IsAny<Guid>(), It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_CorrectCurrentPassword_StoresNewHash()
        {
            var user = CreateStoredUser("correct-current-password");
            _users.Setup(x => x.GetUserById(user.Id)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "correct-current-password",
                NewPassword = "brand-new-password",
                ConfirmPassword = "brand-new-password"
            };

            var result = await _service.ChangePasswordAsync(user.Id, dto);

            Assert.True(result);
            Assert.True(BCrypt.Net.BCrypt.Verify("brand-new-password", user.PasswordHash));
            Assert.False(BCrypt.Net.BCrypt.Verify("correct-current-password", user.PasswordHash));
            _logs.Verify(
                x => x.LogAsync("PASSWORD_CHANGED", It.IsAny<string>(), user.Id, user.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_UnknownUser_ReturnsFalse()
        {
            _users.Setup(x => x.GetUserById(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var dto = new ChangePasswordDto
            {
                CurrentPassword = "current-1",
                NewPassword = "brand-new-password",
                ConfirmPassword = "brand-new-password"
            };

            var result = await _service.ChangePasswordAsync(Guid.NewGuid(), dto);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteUser_SoftDeletesOnceThenReturnsFalse()
        {
            var user = CreateStoredUser("password");
            _users.Setup(x => x.GetUserById(user.Id)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);

            var firstResult = await _service.DeleteUser(user.Id);
            var secondResult = await _service.DeleteUser(user.Id);

            Assert.True(firstResult);
            Assert.True(user.IsDeleted);
            Assert.False(secondResult);
            _logs.Verify(
                x => x.LogAsync("USER_DELETED", It.IsAny<string>(), user.Id, user.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task InviteUserAsync_DuplicateEmail_Throws()
        {
            _users.Setup(x => x.GetByEmailAsync("jane@acme.com")).ReturnsAsync(CreateStoredUser("password"));

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.InviteUserAsync(Guid.NewGuid(), new InviteUserDto
                {
                    Email = "jane@acme.com",
                    FullName = "Jane Member",
                    Role = "Member"
                }));

            Assert.Equal("Email is already registered.", ex.Message);
        }

        [Fact]
        public async Task InviteUserAsync_NewUser_StartsInactiveWithRandomPassword()
        {
            var tenantId = Guid.NewGuid();
            _users.Setup(x => x.GetByEmailAsync("new@acme.com")).ReturnsAsync((User?)null);
            _users.Setup(x => x.CreateUser(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);

            var created = await _service.InviteUserAsync(tenantId, new InviteUserDto
            {
                Email = "new@acme.com",
                FullName = "New Member",
                Role = "Member"
            });

            Assert.Equal(tenantId, created.TenantId);
            Assert.False(created.IsActive); // stays inactive until activation
            Assert.False(string.IsNullOrWhiteSpace(created.EmailVerificationToken));
            // Random temporary password, stored only as a hash.
            Assert.StartsWith("$2", created.PasswordHash);
            _logs.Verify(
                x => x.LogAsync("USER_INVITED", It.IsAny<string>(), created.Id, tenantId),
                Times.Once);
        }

        [Fact]
        public async Task ToggleUserStatusAsync_DeactivatesAndAudits()
        {
            var user = CreateStoredUser("password");
            user.IsActive = true;
            _users.Setup(x => x.GetUserById(user.Id)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);

            var result = await _service.ToggleUserStatusAsync(user.Id, isActive: false);

            Assert.True(result);
            Assert.False(user.IsActive);
            _logs.Verify(
                x => x.LogAsync("USER_DEACTIVATED", It.IsAny<string>(), user.Id, user.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task GetUsersPage_PassesFiltersAndPagingToRepository()
        {
            var tenantId = Guid.NewGuid();
            var expected = new PagedResult<User>
            {
                Data = new List<User> { CreateStoredUser("password") },
                TotalCount = 42,
                Page = 2,
                PageSize = 10
            };
            // Soft-delete, search and role filtering now happen in the database query.
            _users.Setup(x => x.GetUsersPage(tenantId, "jane", "member", true, 2, 10))
                .ReturnsAsync(expected);

            var result = await _service.GetUsersPage(tenantId, "jane", "member", true, 2, 10);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task GetPlatformUsersPage_UsesNullTenantToLoadEveryTenant()
        {
            var expected = new PagedResult<User>();
            _users.Setup(x => x.GetUsersPage(null, null, null, null, 1, 20))
                .ReturnsAsync(expected);

            var result = await _service.GetPlatformUsersPage(null, null, null, 1, 20);

            Assert.Same(expected, result);
            _users.Verify(x => x.GetUsersPage(It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task GetUserById_SoftDeletedUser_ReturnsNull()
        {
            var user = CreateStoredUser("password");
            user.IsDeleted = true;
            _users.Setup(x => x.GetUserById(user.Id)).ReturnsAsync(user);

            var result = await _service.GetUserById(user.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateProfileAsync_OnlyUpdatesProfileFields()
        {
            var user = CreateStoredUser("password");
            _users.Setup(x => x.GetUserById(user.Id)).ReturnsAsync(user);
            _users.Setup(x => x.UpdateUser(user.Id, It.IsAny<User>())).ReturnsAsync(true);

            var result = await _service.UpdateProfileAsync(user.Id, new UpdateProfileDto
            {
                FullName = "Jane Updated",
                ProfileImageUrl = "/uploads/jane.png"
            });

            Assert.True(result);
            Assert.Equal("Jane Updated", user.FullName);
            Assert.Equal("/uploads/jane.png", user.ProfileImageUrl);
            // Email and role are never changed through the profile update.
            Assert.Equal("jane@acme.com", user.Email);
            Assert.Equal("Member", user.Role);
        }

        [Fact]
        public async Task CreateUser_WhenActiveUserLimitReached_ThrowsInvalidOperationException()
        {
            var tenantId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            var tenants = new Mock<ITenantRepository>();
            var plans = new Mock<ISubscriptionPlanRepository>();
            var service = new UserService(_users.Object, _logs.Object, tenants.Object, plans.Object);

            tenants.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(new SaaSPlatform_Model.Entities.Tenant
            {
                Id = tenantId,
                Name = "Acme Corp",
                SubscriptionPlanId = planId
            });

            plans.Setup(x => x.GetByIdAsync(planId)).ReturnsAsync(new SaaSPlatform.Domain.Entities.SubscriptionPlan
            {
                Id = planId,
                Name = "Starter",
                MaxUsers = 5
            });

            _users.Setup(x => x.CountActiveUsersByTenantAsync(tenantId)).ReturnsAsync(5);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUser(new User
            {
                FullName = "New User",
                Email = "new@acme.com",
                PasswordHash = "password",
                TenantId = tenantId
            }));

            Assert.Contains("limit of 5 user(s)", ex.Message);
            _users.Verify(x => x.CreateUser(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task InviteUserAsync_WhenActiveUserLimitReached_ThrowsInvalidOperationException()
        {
            var tenantId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            var tenants = new Mock<ITenantRepository>();
            var plans = new Mock<ISubscriptionPlanRepository>();
            var service = new UserService(_users.Object, _logs.Object, tenants.Object, plans.Object);

            tenants.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(new SaaSPlatform_Model.Entities.Tenant
            {
                Id = tenantId,
                Name = "Acme Corp",
                SubscriptionPlanId = planId
            });

            plans.Setup(x => x.GetByIdAsync(planId)).ReturnsAsync(new SaaSPlatform.Domain.Entities.SubscriptionPlan
            {
                Id = planId,
                Name = "Starter",
                MaxUsers = 3
            });

            _users.Setup(x => x.CountActiveUsersByTenantAsync(tenantId)).ReturnsAsync(3);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.InviteUserAsync(tenantId, new InviteUserDto
            {
                Email = "invited@acme.com",
                FullName = "Invited Member",
                Role = "Member"
            }));

            Assert.Contains("limit of 3 user(s)", ex.Message);
            _users.Verify(x => x.CreateUser(It.IsAny<User>()), Times.Never);
        }
    }
}
