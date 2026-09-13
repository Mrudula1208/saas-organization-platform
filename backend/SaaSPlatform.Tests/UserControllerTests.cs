using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.API.Controllers;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Users;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Utility;
using SaaSPlatform_Model;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _users = new();
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _controller = new UserController(_users.Object);
        }

        [Fact]
        public async Task GetUsers_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller); // authenticated but no tenant claim

            var result = await _controller.GetUsers();

            Assert.IsType<UnauthorizedResult>(result.Result);
            _users.Verify(x => x.GetUsersPage(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetUsers_WithTenantClaim_OnlyLoadsThatTenant()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _users.Setup(x => x.GetUsersPage(tenantId, null, null, null, 1, 20))
                .ReturnsAsync(new PagedResult<User>());

            var result = await _controller.GetUsers();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<PagedResult<User>>(ok.Value);
            _users.Verify(x => x.GetUsersPage(tenantId, null, null, null, 1, 20), Times.Once);
        }

        [Fact]
        public async Task GetUserById_UnknownUser_ReturnsNotFound()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());
            _users.Setup(x => x.GetUserById(It.IsAny<Guid>())).ReturnsAsync((User?)null);

            var result = await _controller.GetUserById(Guid.NewGuid());

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            var body = Assert.IsType<ApiResponse<User>>(notFound.Value);
            Assert.False(body.Success);
            Assert.Equal("User not found", body.Message);
        }

        [Fact]
        public async Task UpdateUser_UserFromAnotherTenant_ReturnsForbidden()
        {
            var callerTenant = Guid.NewGuid();
            var foreignUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Other Tenant User",
                Email = "other@other.com",
                Role = "Member",
                TenantId = Guid.NewGuid() // different tenant
            };
            TestHelpers.SetUser(_controller, tenantId: callerTenant);
            _users.Setup(x => x.GetUserById(foreignUser.Id)).ReturnsAsync(foreignUser);

            var result = await _controller.UpdateUser(foreignUser.Id, new UpdateUserDto
            {
                Name = "Hacked",
                Email = "hacked@other.com",
                Role = "SuperAdmin"
            });

            // Tenant admins can never edit users of another tenant, not even to escalate the role.
            Assert.IsType<ForbidResult>(result.Result);
            _users.Verify(x => x.UpdateUser(It.IsAny<Guid>(), It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ChangePassword_MismatchedPasswords_ReturnsBadRequestWithMessage()
        {
            TestHelpers.SetUser(_controller, userId: Guid.NewGuid());
            _users.Setup(x => x.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordDto>()))
                .ThrowsAsync(new InvalidOperationException("New password and confirmation do not match."));

            var result = await _controller.ChangePassword(new ChangePasswordDto
            {
                CurrentPassword = "current-1",
                NewPassword = "new-password-1",
                ConfirmPassword = "different"
            });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var body = Assert.IsType<ApiResponse<object>>(badRequest.Value);
            Assert.False(body.Success);
            Assert.Equal("New password and confirmation do not match.", body.Message);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequestWithMessage()
        {
            TestHelpers.SetUser(_controller, userId: Guid.NewGuid());
            _users.Setup(x => x.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordDto>()))
                .ThrowsAsync(new InvalidOperationException("Current password is incorrect."));

            var result = await _controller.ChangePassword(new ChangePasswordDto
            {
                CurrentPassword = "wrong",
                NewPassword = "new-password-1",
                ConfirmPassword = "new-password-1"
            });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var body = Assert.IsType<ApiResponse<object>>(badRequest.Value);
            Assert.Equal("Current password is incorrect.", body.Message);
        }

        [Fact]
        public async Task ChangePassword_Success_ReturnsOk()
        {
            TestHelpers.SetUser(_controller, userId: Guid.NewGuid());
            _users.Setup(x => x.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordDto>()))
                .ReturnsAsync(true);

            var result = await _controller.ChangePassword(new ChangePasswordDto
            {
                CurrentPassword = "current-1",
                NewPassword = "new-password-1",
                ConfirmPassword = "new-password-1"
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ApiResponse<object>>(ok.Value);
            Assert.True(body.Success);
        }

        [Fact]
        public async Task GetProfile_MissingUserClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller); // no NameIdentifier claim

            var result = await _controller.GetProfile();

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetProfile_Success_ReturnsProfile()
        {
            var userId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, userId: userId);
            _users.Setup(x => x.GetProfileAsync(userId)).ReturnsAsync(new UserProfileDto
            {
                Id = userId,
                FullName = "Jane Doe",
                Email = "jane@tenant.com",
                Role = "TenantAdmin"
            });

            var result = await _controller.GetProfile();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ApiResponse<UserProfileDto>>(ok.Value);
            Assert.True(body.Success);
            Assert.Equal("Jane Doe", body.Data!.FullName);
        }

        [Fact]
        public async Task UpdateProfile_Success_ReturnsUpdatedProfile()
        {
            var userId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, userId: userId);
            _users.Setup(x => x.UpdateProfileAsync(userId, It.IsAny<UpdateProfileDto>())).ReturnsAsync(true);
            _users.Setup(x => x.GetProfileAsync(userId)).ReturnsAsync(new UserProfileDto
            {
                Id = userId,
                FullName = "Updated Name",
                Email = "jane@tenant.com",
                ProfileImageUrl = "https://example.com/avatar.png"
            });

            var result = await _controller.UpdateProfile(new UpdateProfileDto
            {
                FullName = "Updated Name",
                ProfileImageUrl = "https://example.com/avatar.png"
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ApiResponse<UserProfileDto>>(ok.Value);
            Assert.True(body.Success);
            Assert.Equal("Updated Name", body.Data!.FullName);
        }

        [Fact]
        public async Task ToggleStatus_UnknownUser_ReturnsNotFound()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());
            _users.Setup(x => x.ToggleUserStatusAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(false);

            var result = await _controller.ToggleStatus(Guid.NewGuid(), new ToggleStatusRequest { IsActive = false });

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
