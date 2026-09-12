using Moq;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform.Domain.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _notifications = new();
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _service = new NotificationService(_notifications.Object);
        }

        [Fact]
        public async Task CreateAsync_AssignsTenantAndStartsUnread()
        {
            var tenantId = Guid.NewGuid();
            _notifications.Setup(x => x.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);

            var created = await _service.CreateAsync(tenantId, "Task assigned to you");

            Assert.Equal(tenantId, created.TenantId);
            Assert.False(created.IsRead);
            Assert.NotEqual(Guid.Empty, created.Id);
            _notifications.Verify(x => x.AddAsync(created), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_PassesCallerTenantToRepository()
        {
            var tenantId = Guid.NewGuid();
            _notifications.Setup(x => x.GetAllAsync(tenantId)).ReturnsAsync(new List<Notification>());

            await _service.GetAllAsync(tenantId);

            _notifications.Verify(x => x.GetAllAsync(tenantId), Times.Once);
            _notifications.Verify(x => x.GetAllAsync(It.Is<Guid>(id => id != tenantId)), Times.Never);
        }

        [Fact]
        public async Task MarkReadAsync_OwnNotification_MarksRead()
        {
            var tenantId = Guid.NewGuid();
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Message = "Hello",
                IsRead = false
            };
            _notifications.Setup(x => x.GetByIdAsync(notification.Id)).ReturnsAsync(notification);
            _notifications.Setup(x => x.UpdateAsync(notification)).Returns(Task.CompletedTask);

            var result = await _service.MarkReadAsync(notification.Id, tenantId);

            Assert.True(result);
            Assert.True(notification.IsRead);
        }

        [Fact]
        public async Task MarkReadAsync_NotificationFromAnotherTenant_ReturnsFalseAndChangesNothing()
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(), // tenant B
                Message = "Hello",
                IsRead = false
            };
            _notifications.Setup(x => x.GetByIdAsync(notification.Id)).ReturnsAsync(notification);

            // Tenant A tries to mark tenant B's notification as read.
            var result = await _service.MarkReadAsync(notification.Id, Guid.NewGuid());

            Assert.False(result);
            Assert.False(notification.IsRead);
            _notifications.Verify(x => x.UpdateAsync(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_NotificationFromAnotherTenant_ReturnsFalse()
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Message = "Hello"
            };
            _notifications.Setup(x => x.GetByIdAsync(notification.Id)).ReturnsAsync(notification);

            var result = await _service.DeleteAsync(notification.Id, Guid.NewGuid());

            Assert.False(result);
            _notifications.Verify(x => x.DeleteAsync(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_OwnNotification_Deletes()
        {
            var tenantId = Guid.NewGuid();
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Message = "Hello"
            };
            _notifications.Setup(x => x.GetByIdAsync(notification.Id)).ReturnsAsync(notification);
            _notifications.Setup(x => x.DeleteAsync(notification)).Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync(notification.Id, tenantId);

            Assert.True(result);
            _notifications.Verify(x => x.DeleteAsync(notification), Times.Once);
        }

        [Fact]
        public async Task GetUnreadCountAsync_ReturnsRepositoryCount()
        {
            var tenantId = Guid.NewGuid();
            _notifications.Setup(x => x.GetUnreadCountAsync(tenantId)).ReturnsAsync(7);

            var count = await _service.GetUnreadCountAsync(tenantId);

            Assert.Equal(7, count);
        }

        [Fact]
        public async Task MarkAllReadAsync_CallsRepository()
        {
            var tenantId = Guid.NewGuid();
            _notifications.Setup(x => x.MarkAllReadAsync(tenantId)).Returns(Task.CompletedTask);

            var result = await _service.MarkAllReadAsync(tenantId);

            Assert.True(result);
            _notifications.Verify(x => x.MarkAllReadAsync(tenantId), Times.Once);
        }

        [Fact]
        public async Task ClearAllAsync_CallsRepository()
        {
            var tenantId = Guid.NewGuid();
            _notifications.Setup(x => x.ClearAllAsync(tenantId)).Returns(Task.CompletedTask);

            var result = await _service.ClearAllAsync(tenantId);

            Assert.True(result);
            _notifications.Verify(x => x.ClearAllAsync(tenantId), Times.Once);
        }
    }
}
