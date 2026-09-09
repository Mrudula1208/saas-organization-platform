using Moq;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform_Model;
using SaaSPlatform.Domain.Entities;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class ProjectServiceTests
    {
        private readonly Mock<IProjectRepository> _projects = new();
        private readonly Mock<ISystemLogRepository> _logs = new();
        private readonly ProjectService _service;

        public ProjectServiceTests()
        {
            _service = new ProjectService(_projects.Object, _logs.Object);
        }

        private static Project CreateProject(Guid tenantId, string name = "Website", string status = "Active")
        {
            return new Project
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = "",
                TenantId = tenantId,
                OwnerId = Guid.NewGuid(),
                Status = status,
                Priority = "Medium",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true,
                IsDeleted = false
            };
        }

        [Fact]
        public async Task CreateAsync_MissingName_Throws()
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(new CreateProjectDto { Name = "  " }));

            Assert.Equal("Project name is required.", ex.Message);
            _projects.Verify(x => x.AddAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_InvalidStatus_Throws()
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(new CreateProjectDto { Name = "Website", Status = "Almost Done" }));

            Assert.Contains("Invalid project status", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_EndDateBeforeStartDate_Throws()
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(new CreateProjectDto
                {
                    Name = "Website",
                    StartDate = new DateTime(2026, 5, 1),
                    EndDate = new DateTime(2026, 4, 1)
                }));

            Assert.Equal("End date cannot be before the start date.", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_ValidProject_TrimsNameAndAppliesDefaults()
        {
            var tenantId = Guid.NewGuid();
            _projects.Setup(x => x.AddAsync(It.IsAny<Project>()))
                .ReturnsAsync((Project p) => p);

            var dto = new CreateProjectDto
            {
                Name = "  Website  ",
                Description = "Marketing site",
                TenantId = tenantId,
                OwnerId = Guid.NewGuid(),
                Status = "", // no status chosen yet
                Priority = "High"
            };

            var created = await _service.CreateAsync(dto);

            Assert.Equal("Website", created.Name);
            Assert.Equal("Pending", created.Status);
            Assert.Equal("High", created.Priority);
            Assert.Equal(tenantId, created.TenantId);
            _logs.Verify(
                x => x.LogAsync("PROJECT_CREATED", It.IsAny<string>(), It.IsAny<Guid>(), tenantId),
                Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ProjectFromAnotherTenant_ReturnsNull()
        {
            var project = CreateProject(Guid.NewGuid());
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            // A different tenant asks for this project.
            var result = await _service.GetByIdAsync(project.Id, Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_SoftDeletedProject_ReturnsNull()
        {
            var project = CreateProject(Guid.NewGuid());
            project.IsDeleted = true;
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            var result = await _service.GetByIdAsync(project.Id, project.TenantId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_OwnProject_ComputesTaskProgress()
        {
            var project = CreateProject(Guid.NewGuid());
            project.Tasks = new List<TaskItem>
            {
                new() { Id = Guid.NewGuid(), Name = "T1", IsCompleted = false, Status = "To Do" },
                new() { Id = Guid.NewGuid(), Name = "T2", IsCompleted = true, Status = "Completed" }
            };
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            var result = await _service.GetByIdAsync(project.Id, project.TenantId);

            Assert.NotNull(result);
            Assert.Equal(2, result!.TaskCount);
            Assert.Equal(1, result.CompletedTaskCount);
            Assert.Equal(50, result.Progress);
        }

        [Fact]
        public async Task UpdateAsync_UnknownProject_ThrowsKeyNotFound()
        {
            _projects.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Project?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateProjectDto { Name = "New" }));
        }

        [Fact]
        public async Task UpdateAsync_ProjectFromAnotherTenant_ThrowsUnauthorized()
        {
            var project = CreateProject(Guid.NewGuid());
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateAsync(project.Id, Guid.NewGuid(), new UpdateProjectDto { Name = "New" }));

            Assert.Equal("You do not have access to this project.", ex.Message);
            _projects.Verify(x => x.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_InvalidPriority_Throws()
        {
            var project = CreateProject(Guid.NewGuid());
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateAsync(project.Id, project.TenantId, new UpdateProjectDto
                {
                    Name = "Website",
                    Priority = "Urgent"
                }));

            Assert.Contains("Invalid project priority", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_TooLongName_Throws()
        {
            var project = CreateProject(Guid.NewGuid());
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateAsync(project.Id, project.TenantId, new UpdateProjectDto
                {
                    Name = new string('a', 201),
                    Status = "Active",
                    Priority = "Medium"
                }));

            Assert.Contains("200 characters", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_OwnProject_UpdatesFieldsAndKeepsExistingWhenBlank()
        {
            var project = CreateProject(Guid.NewGuid(), status: "In Progress");
            project.Priority = "High";
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);
            _projects.Setup(x => x.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

            await _service.UpdateAsync(project.Id, project.TenantId, new UpdateProjectDto
            {
                Name = "Website Redesign",
                Description = "New look",
                Status = "", // blank keeps the current status
                Priority = "", // blank keeps the current priority
                IsActive = false
            });

            Assert.Equal("Website Redesign", project.Name);
            Assert.Equal("In Progress", project.Status);
            Assert.Equal("High", project.Priority);
            Assert.False(project.IsActive);
            _logs.Verify(
                x => x.LogAsync("PROJECT_UPDATED", It.IsAny<string>(), It.IsAny<Guid>(), project.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ProjectFromAnotherTenant_ThrowsUnauthorized()
        {
            var project = CreateProject(Guid.NewGuid());
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.DeleteAsync(project.Id, Guid.NewGuid()));

            Assert.False(project.IsDeleted);
            _projects.Verify(x => x.UpdateAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_OwnProject_SoftDeletes()
        {
            var project = CreateProject(Guid.NewGuid());
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);
            _projects.Setup(x => x.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);

            await _service.DeleteAsync(project.Id, project.TenantId);

            Assert.True(project.IsDeleted);
            Assert.False(project.IsActive);
            _logs.Verify(
                x => x.LogAsync("PROJECT_DELETED", It.IsAny<string>(), It.IsAny<Guid>(), project.TenantId),
                Times.Once);
        }

        [Fact]
        public async Task GetProjectsPage_MapsEntitiesAndKeepsPagingInfo()
        {
            var tenantId = Guid.NewGuid();
            // Deleted rows and the status/search filters are handled by the database query.
            var completed = CreateProject(tenantId, "Done Project", "Completed");
            _projects.Setup(x => x.GetProjectsPage(tenantId, "done", "completed", null, 2, 20))
                .ReturnsAsync(new PagedResult<Project>
                {
                    Data = new List<Project> { completed },
                    TotalCount = 7,
                    Page = 2,
                    PageSize = 20
                });

            var result = await _service.GetProjectsPage(tenantId, "done", "completed", null, 2, 20);

            var only = Assert.Single(result.Data);
            Assert.Equal("Done Project", only.Name);
            Assert.Equal(7, result.TotalCount);
            Assert.Equal(2, result.Page);
            Assert.Equal(20, result.PageSize);
        }

        [Fact]
        public async Task CreateAsync_WhenProjectLimitReached_ThrowsInvalidOperationException()
        {
            var tenantId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            var tenants = new Mock<ITenantRepository>();
            var plans = new Mock<ISubscriptionPlanRepository>();
            var service = new ProjectService(_projects.Object, _logs.Object, tenants.Object, plans.Object);

            tenants.Setup(x => x.GetByIdAsync(tenantId)).ReturnsAsync(new SaaSPlatform_Model.Entities.Tenant
            {
                Id = tenantId,
                Name = "Acme Corp",
                SubscriptionPlanId = planId
            });

            plans.Setup(x => x.GetByIdAsync(planId)).ReturnsAsync(new SaaSPlatform.Domain.Entities.SubscriptionPlan
            {
                Id = planId,
                Name = "Basic",
                MaxProjects = 3
            });

            _projects.Setup(x => x.CountActiveProjectsByTenantAsync(tenantId)).ReturnsAsync(3);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(new CreateProjectDto
            {
                Name = "New Project",
                TenantId = tenantId,
                OwnerId = Guid.NewGuid()
            }));

            Assert.Contains("limit of 3 project(s)", ex.Message);
            _projects.Verify(x => x.AddAsync(It.IsAny<Project>()), Times.Never);
        }
    }
}
