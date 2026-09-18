using Moq;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Tasks;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class TaskServiceTests
    {
        private readonly Mock<ITaskRepository> _tasks = new();
        private readonly Mock<IProjectRepository> _projects = new();
        private readonly Mock<ISystemLogRepository> _logs = new();
        private readonly Mock<IUserRepository> _users = new();
        private readonly TaskService _service;

        public TaskServiceTests()
        {
            _service = new TaskService(_tasks.Object, _projects.Object, _logs.Object, _users.Object);
        }

        private static Project CreateProject(Guid tenantId, bool isDeleted = false)
        {
            return new Project
            {
                Id = Guid.NewGuid(),
                Name = "Website",
                Description = "",
                TenantId = tenantId,
                OwnerId = Guid.NewGuid(),
                Status = "Active",
                Priority = "Medium",
                IsActive = true,
                IsDeleted = isDeleted
            };
        }

        private static TaskItem CreateTask(Guid tenantId, string status = "To Do", bool isDeleted = false)
        {
            return new TaskItem
            {
                Id = Guid.NewGuid(),
                Name = "Write spec",
                Description = "",
                ProjectId = Guid.NewGuid(),
                AssignedUserId = Guid.NewGuid(),
                Status = status,
                Priority = "Medium",
                TenantId = tenantId,
                IsDeleted = isDeleted
            };
        }

        [Fact]
        public async Task CreateAsync_MissingName_Throws()
        {
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateAsync(new CreateTaskDto { Name = "" }));

            Assert.Equal("Task name is required.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_UnknownProject_Throws()
        {
            _projects.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Project?)null);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateAsync(new CreateTaskDto { Name = "Task", ProjectId = Guid.NewGuid() }));

            Assert.Equal("Selected project was not found in this tenant.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_ProjectFromAnotherTenant_Throws()
        {
            var project = CreateProject(Guid.NewGuid()); // belongs to tenant B
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            // The request comes in claiming tenant A.
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateAsync(new CreateTaskDto
                {
                    Name = "Task",
                    ProjectId = project.Id,
                    TenantId = Guid.NewGuid()
                }));

            Assert.Equal("Selected project was not found in this tenant.", ex.Message);
            _tasks.Verify(x => x.AddAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_SoftDeletedProject_Throws()
        {
            var project = CreateProject(Guid.NewGuid(), isDeleted: true);
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateAsync(new CreateTaskDto
                {
                    Name = "Task",
                    ProjectId = project.Id,
                    TenantId = project.TenantId
                }));
        }

        [Fact]
        public async Task CreateAsync_ValidTask_UsesProjectTenantAndDefaults()
        {
            var project = CreateProject(Guid.NewGuid());
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);
            _tasks.Setup(x => x.AddAsync(It.IsAny<TaskItem>()))
                .ReturnsAsync((TaskItem t) => t);

            var created = await _service.CreateAsync(new CreateTaskDto
            {
                Name = "Write spec",
                ProjectId = project.Id,
                TenantId = project.TenantId,
                Status = null!, // not provided: the service falls back to "To Do"
                Priority = null! // not provided: the service falls back to "Medium"
            });

            // The tenant always comes from the project itself.
            Assert.Equal(project.TenantId, created.TenantId);
            Assert.Equal("To Do", created.Status);
            Assert.Equal("Medium", created.Priority);
            Assert.False(created.IsCompleted);
            _logs.Verify(
                x => x.LogAsync("TASK_CREATED", It.IsAny<string>(), It.IsAny<Guid>(), project.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UnknownTask_Throws()
        {
            _tasks.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.UpdateAsync(Guid.NewGuid(), new UpdateTaskDto { Name = "Task" }));

            Assert.Equal("Task not found.", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_CompletedStatus_MarksTaskCompleteWithTimestamp()
        {
            var task = CreateTask(Guid.NewGuid());
            _tasks.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);
            _tasks.Setup(x => x.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

            await _service.UpdateAsync(task.Id, new UpdateTaskDto
            {
                Name = task.Name,
                Status = "Completed",
                IsCompleted = false // even when the client sends false, the status wins
            });

            Assert.True(task.IsCompleted);
            Assert.NotNull(task.CompletedAt);
        }

        [Fact]
        public async Task UpdateAsync_InProgressStatus_ClearsCompletion()
        {
            var task = CreateTask(Guid.NewGuid(), status: "In Progress");
            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;
            _tasks.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);
            _tasks.Setup(x => x.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

            await _service.UpdateAsync(task.Id, new UpdateTaskDto
            {
                Name = task.Name,
                Status = "In Progress",
                IsCompleted = true // client lies, but status decides
            });

            Assert.False(task.IsCompleted);
            Assert.Null(task.CompletedAt);
        }

        [Fact]
        public async Task UpdateStatusAsync_UnknownTask_ReturnsFalse()
        {
            _tasks.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);

            var result = await _service.UpdateStatusAsync(Guid.NewGuid(), "Completed");

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateStatusAsync_ToCompleted_MarksCompleteAndAuditsOldStatus()
        {
            var task = CreateTask(Guid.NewGuid(), status: "To Do");
            _tasks.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);
            _tasks.Setup(x => x.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

            var result = await _service.UpdateStatusAsync(task.Id, "Completed");

            Assert.True(result);
            Assert.Equal("Completed", task.Status);
            Assert.True(task.IsCompleted);
            Assert.NotNull(task.CompletedAt);
            _logs.Verify(
                x => x.LogAsync(
                    "TASK_STATUS_UPDATED",
                    It.Is<string>(m => m.Contains("To Do") && m.Contains("Completed")),
                    It.IsAny<Guid>(),
                    task.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_BackToTodo_ClearsCompletion()
        {
            var task = CreateTask(Guid.NewGuid(), status: "Completed");
            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;
            _tasks.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);
            _tasks.Setup(x => x.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

            var result = await _service.UpdateStatusAsync(task.Id, "To Do");

            Assert.True(result);
            Assert.False(task.IsCompleted);
            Assert.Null(task.CompletedAt);
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesThenGetByIdReturnsNull()
        {
            var task = CreateTask(Guid.NewGuid());
            _tasks.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);
            _tasks.Setup(x => x.UpdateAsync(It.IsAny<TaskItem>())).Returns(Task.CompletedTask);

            await _service.DeleteAsync(task.Id);

            Assert.True(task.IsDeleted);
            _logs.Verify(
                x => x.LogAsync("TASK_DELETED", It.IsAny<string>(), It.IsAny<Guid>(), task.TenantId),
                Times.Once);

            // A deleted task can no longer be loaded.
            var loaded = await _service.GetByIdAsync(task.Id);
            Assert.Null(loaded);
        }

        [Fact]
        public async Task GetTasksPage_CallsRepositoryAndClearsProjectTasksForJson()
        {
            var tenantId = Guid.NewGuid();
            // Deleted rows and the status/search filters are handled by the database query.
            var task = CreateTask(tenantId, "To Do");
            task.Project = CreateProject(tenantId);
            task.Project.Tasks = new List<TaskItem> { task };
            _tasks.Setup(x => x.GetTasksPage(tenantId, null, "to do", "spec", 1, 20))
                .ReturnsAsync(new PagedResult<TaskItem>
                {
                    Data = new List<TaskItem> { task },
                    TotalCount = 3,
                    Page = 1,
                    PageSize = 20
                });

            var result = await _service.GetTasksPage(tenantId, null, "to do", "spec", 1, 20);

            var only = Assert.Single(result.Data);
            Assert.Equal(task.Id, only.Id);
            Assert.Equal(3, result.TotalCount);
            // The project's task collection is cleared so the JSON response has no reference cycle.
            Assert.Null(only.Project.Tasks);
        }

        [Fact]
        public async Task CreateAsync_AssignedUserFromAnotherTenant_Throws()
        {
            var tenantId = Guid.NewGuid();
            var foreignTenantId = Guid.NewGuid();
            var project = CreateProject(tenantId);
            var foreignUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Foreign User",
                Email = "foreign@other.com",
                TenantId = foreignTenantId,
                IsActive = true
            };

            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);
            _users.Setup(x => x.GetUserById(foreignUser.Id)).ReturnsAsync(foreignUser);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.CreateAsync(new CreateTaskDto
                {
                    Name = "Cross-tenant task",
                    ProjectId = project.Id,
                    TenantId = tenantId,
                    AssignedUserId = foreignUser.Id
                }));

            Assert.Equal("Assigned user was not found in this tenant.", ex.Message);
            _tasks.Verify(x => x.AddAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_AssignedUserFromAnotherTenant_Throws()
        {
            var tenantId = Guid.NewGuid();
            var foreignTenantId = Guid.NewGuid();
            var task = CreateTask(tenantId);
            var foreignUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Foreign User",
                Email = "foreign@other.com",
                TenantId = foreignTenantId,
                IsActive = true
            };

            _tasks.Setup(x => x.GetByIdAsync(task.Id)).ReturnsAsync(task);
            _users.Setup(x => x.GetUserById(foreignUser.Id)).ReturnsAsync(foreignUser);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.UpdateAsync(task.Id, new UpdateTaskDto
                {
                    Name = task.Name,
                    AssignedUserId = foreignUser.Id
                }));

            Assert.Equal("Assigned user was not found in this tenant.", ex.Message);
            _tasks.Verify(x => x.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
        }
    }
}
