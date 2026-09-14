using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.API.Controllers;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Tasks;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class TasksControllerTests
    {
        private readonly Mock<ITaskService> _tasks = new();
        private readonly TasksController _controller;

        public TasksControllerTests()
        {
            _controller = new TasksController(_tasks.Object);
        }

        [Fact]
        public async Task GetAllTasks_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller);

            var result = await _controller.GetAllTasks();

            Assert.IsType<UnauthorizedResult>(result.Result);
            _tasks.Verify(x => x.GetTasksPage(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetAllTasks_WithTenantClaim_OnlyLoadsThatTenant()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _tasks.Setup(x => x.GetTasksPage(tenantId, null, null, null, 1, 20)).ReturnsAsync(new PagedResult<TaskItem>());

            var result = await _controller.GetAllTasks();

            Assert.IsType<OkObjectResult>(result.Result);
            _tasks.Verify(x => x.GetTasksPage(tenantId, null, null, null, 1, 20), Times.Once);
        }

        [Fact]
        public async Task Create_TenantIdComesFromClaimNotBody()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);

            // The client tries to create the task under another tenant.
            var dto = new CreateTaskDto { Name = "Write spec", TenantId = Guid.NewGuid() };
            _tasks.Setup(x => x.CreateAsync(It.IsAny<CreateTaskDto>()))
                .ReturnsAsync(new TaskItem { Id = Guid.NewGuid(), Name = dto.Name, TenantId = tenantId });

            var result = await _controller.Create(dto);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            _tasks.Verify(
                x => x.CreateAsync(It.Is<CreateTaskDto>(d => d.TenantId == tenantId)),
                Times.Once);
        }

        [Fact]
        public async Task GetById_TaskFromAnotherTenant_ReturnsNotFound()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _tasks.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new TaskItem { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() });

            var result = await _controller.GetById(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateStatus_TaskFromAnotherTenant_ReturnsNotFound()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            // Another tenant's task must not even be visible for status changes.
            _tasks.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new TaskItem { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() });

            var result = await _controller.UpdateStatus(Guid.NewGuid(), new UpdateTaskStatusDto { Status = "Completed" });

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Task not found.", TestHelpers.ReadMessage(notFound.Value));
            _tasks.Verify(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatus_OwnTask_UpdatesAndReturnsOk()
        {
            var tenantId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _tasks.Setup(x => x.GetByIdAsync(taskId))
                .ReturnsAsync(new TaskItem { Id = taskId, TenantId = tenantId });
            _tasks.Setup(x => x.UpdateStatusAsync(taskId, "Completed")).ReturnsAsync(true);

            var result = await _controller.UpdateStatus(taskId, new UpdateTaskStatusDto { Status = "Completed" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(TestHelpers.ReadMessage(ok.Value)?.Contains("updated") == true);
        }

        [Fact]
        public async Task Update_TaskFromAnotherTenant_ReturnsNotFound()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _tasks.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new TaskItem { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() });

            var result = await _controller.Update(Guid.NewGuid(), new UpdateTaskDto { Name = "Hacked" });

            Assert.IsType<NotFoundResult>(result);
            _tasks.Verify(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateTaskDto>()), Times.Never);
        }

        [Fact]
        public async Task Delete_TaskFromAnotherTenant_ReturnsNotFound()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _tasks.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new TaskItem { Id = Guid.NewGuid(), TenantId = Guid.NewGuid() });

            var result = await _controller.Delete(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
            _tasks.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
