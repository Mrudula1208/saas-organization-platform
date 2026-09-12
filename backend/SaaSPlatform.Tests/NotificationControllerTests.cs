using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.API.Controllers;
using SaaSPlatform.Application.DTOS.Notifications;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Utility;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class NotificationControllerTests
    {
        private readonly Mock<INotificationService> _notifications = new();
        private readonly NotificationController _controller;

        public NotificationControllerTests()
        {
            _controller = new NotificationController(_notifications.Object);
        }

        [Fact]
        public async Task GetAll_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller); // authenticated but no tenant claim

            var result = await _controller.GetAll();

            Assert.IsType<UnauthorizedResult>(result);
            _notifications.Verify(x => x.GetAllAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetAll_WithTenantClaim_OnlyLoadsThatTenant()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _notifications.Setup(x => x.GetAllAsync(tenantId)).ReturnsAsync(new List<Notification>());

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ApiResponse<object>>(ok.Value);
            Assert.True(body.Success);
            _notifications.Verify(x => x.GetAllAsync(tenantId), Times.Once);
            _notifications.Verify(x => x.GetAllAsync(It.Is<Guid>(id => id != tenantId)), Times.Never);
        }

        [Fact]
        public async Task GetUnreadCount_WithTenantClaim_UsesClaimTenant()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _notifications.Setup(x => x.GetUnreadCountAsync(tenantId)).ReturnsAsync(3);

            var result = await _controller.GetUnreadCount();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ApiResponse<object>>(ok.Value);
            Assert.Equal(3, body.Data);
        }

        [Fact]
        public async Task Create_EmptyMessage_ReturnsBadRequestAndSkipsService()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());

            var result = await _controller.Create(new CreateNotificationDto { Message = "   " });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var body = Assert.IsType<ApiResponse<object>>(badRequest.Value);
            Assert.False(body.Success);
            Assert.Equal("Message is required.", body.Message);
            _notifications.Verify(x => x.CreateAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Create_WithTenantClaim_CreatesUnderClaimTenant()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _notifications.Setup(x => x.CreateAsync(tenantId, "Hello"))
                .ReturnsAsync(new Notification { Id = Guid.NewGuid(), TenantId = tenantId, Message = "Hello" });

            var result = await _controller.Create(new CreateNotificationDto { Message = "Hello" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ApiResponse<object>>(ok.Value);
            Assert.True(body.Success);
            _notifications.Verify(x => x.CreateAsync(tenantId, "Hello"), Times.Once);
        }

        [Fact]
        public async Task MarkRead_UnknownNotification_ReturnsNotFound()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());
            _notifications.Setup(x => x.MarkReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

            var result = await _controller.MarkRead(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task MarkRead_OwnNotification_ReturnsOk()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            var notificationId = Guid.NewGuid();
            _notifications.Setup(x => x.MarkReadAsync(notificationId, tenantId)).ReturnsAsync(true);

            var result = await _controller.MarkRead(notificationId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ApiResponse<object>>(ok.Value);
            Assert.True(body.Success);
        }

        [Fact]
        public async Task Delete_NotificationFromAnotherTenant_ReturnsNotFound()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            var notificationId = Guid.NewGuid();
            // The service refuses cross-tenant deletes, so the controller answers 404.
            _notifications.Setup(x => x.DeleteAsync(notificationId, tenantId)).ReturnsAsync(false);

            var result = await _controller.Delete(notificationId);

            Assert.IsType<NotFoundResult>(result);
            _notifications.Verify(x => x.DeleteAsync(notificationId, tenantId), Times.Once);
        }

        [Fact]
        public async Task MarkAllRead_WithTenantClaim_CallsService()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _notifications.Setup(x => x.MarkAllReadAsync(tenantId)).ReturnsAsync(true);

            var result = await _controller.MarkAllRead();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ApiResponse<object>>(ok.Value);
            Assert.True(body.Success);
            _notifications.Verify(x => x.MarkAllReadAsync(tenantId), Times.Once);
        }

        [Fact]
        public async Task ClearAll_WithTenantClaim_CallsService()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _notifications.Setup(x => x.ClearAllAsync(tenantId)).ReturnsAsync(true);

            var result = await _controller.ClearAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<ApiResponse<object>>(ok.Value);
            Assert.True(body.Success);
            _notifications.Verify(x => x.ClearAllAsync(tenantId), Times.Once);
        }
    }
}
