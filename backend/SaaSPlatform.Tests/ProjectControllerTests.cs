using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.API.Controllers;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.Interfaces;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class ProjectControllerTests
    {
        private readonly Mock<IProjectService> _projects = new();
        private readonly ProjectController _controller;

        public ProjectControllerTests()
        {
            _controller = new ProjectController(_projects.Object);
        }

        [Fact]
        public async Task GetProjects_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller);

            var result = await _controller.GetProjects();

            Assert.IsType<UnauthorizedResult>(result.Result);
            _projects.Verify(x => x.GetProjectsPage(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetById_ProjectFromAnotherTenant_Returns403()
        {
            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);

            // The service hides other tenants' projects, but the project does exist.
            _projects.Setup(x => x.GetByIdAsync(projectId, tenantId)).ReturnsAsync((ProjectViewDto?)null);
            _projects.Setup(x => x.ExistsAsync(projectId)).ReturnsAsync(true);

            var result = await _controller.GetById(projectId);

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetById_UnknownProject_Returns404()
        {
            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);

            _projects.Setup(x => x.GetByIdAsync(projectId, tenantId)).ReturnsAsync((ProjectViewDto?)null);
            _projects.Setup(x => x.ExistsAsync(projectId)).ReturnsAsync(false);

            var result = await _controller.GetById(projectId);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetById_OwnProject_ReturnsOk()
        {
            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _projects.Setup(x => x.GetByIdAsync(projectId, tenantId))
                .ReturnsAsync(new ProjectViewDto { Id = projectId, Name = "Website" });

            var result = await _controller.GetById(projectId);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var project = Assert.IsType<ProjectViewDto>(ok.Value);
            Assert.Equal("Website", project.Name);
        }

        [Fact]
        public async Task Create_TenantAndOwnerComeFromClaimsNotBody()
        {
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId, userId: userId);

            // The client tries to submit another tenant/owner in the payload.
            var dto = new CreateProjectDto
            {
                Name = "Website",
                TenantId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid()
            };
            _projects.Setup(x => x.CreateAsync(It.IsAny<CreateProjectDto>()))
                .ReturnsAsync(new ProjectViewDto { Id = Guid.NewGuid(), Name = dto.Name });

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(created.Value);
            // The values taken from the JWT claims always win.
            _projects.Verify(
                x => x.CreateAsync(It.Is<CreateProjectDto>(d => d.TenantId == tenantId && d.OwnerId == userId)),
                Times.Once);
        }

        [Fact]
        public async Task Create_InvalidInput_ReturnsBadRequestWithMessage()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid(), userId: Guid.NewGuid());
            _projects.Setup(x => x.CreateAsync(It.IsAny<CreateProjectDto>()))
                .ThrowsAsync(new ArgumentException("Project name is required."));

            var result = await _controller.Create(new CreateProjectDto { Name = "" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Project name is required.", TestHelpers.ReadMessage(badRequest.Value));
        }

        [Fact]
        public async Task Update_UnknownProject_Returns404()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());
            _projects.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateProjectDto>()))
                .ThrowsAsync(new KeyNotFoundException("Project not found."));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateProjectDto { Name = "New" });

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_ProjectFromAnotherTenant_Returns403()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _projects.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), tenantId, It.IsAny<UpdateProjectDto>()))
                .ThrowsAsync(new UnauthorizedAccessException("You do not have access to this project."));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateProjectDto { Name = "New" });

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        [Fact]
        public async Task Update_InvalidInput_ReturnsBadRequest()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());
            _projects.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateProjectDto>()))
                .ThrowsAsync(new ArgumentException("Invalid project status 'Nope'."));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateProjectDto { Name = "X", Status = "Nope" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ProjectFromAnotherTenant_Returns403()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _projects.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), tenantId))
                .ThrowsAsync(new UnauthorizedAccessException("You do not have access to this project."));

            var result = await _controller.Delete(Guid.NewGuid());

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }
    }
}
