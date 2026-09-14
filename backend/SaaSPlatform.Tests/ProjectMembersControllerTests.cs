using Microsoft.AspNetCore.Mvc;
using Moq;
using SaaSPlatform.API.Controllers;
using SaaSPlatform.Application.DTOS.ProjectMembers;
using SaaSPlatform.Application.Interfaces;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class ProjectMembersControllerTests
    {
        private readonly Mock<IProjectMemberService> _members = new();
        private readonly ProjectMembersController _controller;

        public ProjectMembersControllerTests()
        {
            _controller = new ProjectMembersController(_members.Object);
        }

        [Fact]
        public async Task GetMembers_MissingTenantClaim_ReturnsUnauthorized()
        {
            TestHelpers.SetUser(_controller);

            var result = await _controller.GetMembersByProject(Guid.NewGuid());

            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetMembers_WithTenantClaim_UsesClaimTenant()
        {
            var tenantId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _members.Setup(x => x.GetMembersByProjectAsync(projectId, tenantId))
                .ReturnsAsync(new List<ProjectMemberDto>());

            var result = await _controller.GetMembersByProject(projectId);

            Assert.IsType<OkObjectResult>(result.Result);
            _members.Verify(x => x.GetMembersByProjectAsync(projectId, tenantId), Times.Once);
        }

        [Fact]
        public async Task AddMember_UnknownProject_ReturnsNotFound()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());
            _members.Setup(x => x.AddMemberAsync(It.IsAny<AddProjectMemberDto>(), It.IsAny<Guid>()))
                .ThrowsAsync(new KeyNotFoundException("Project not found in this tenant."));

            var result = await _controller.AddMember(new AddProjectMemberDto
            {
                ProjectId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            });

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Project not found in this tenant.", TestHelpers.ReadMessage(notFound.Value));
        }

        [Fact]
        public async Task AddMember_Valid_ReturnsCreated()
        {
            var tenantId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _members.Setup(x => x.AddMemberAsync(It.IsAny<AddProjectMemberDto>(), tenantId))
                .ReturnsAsync(new ProjectMemberDto { Id = Guid.NewGuid(), ProjectId = Guid.NewGuid(), UserId = Guid.NewGuid() });

            var result = await _controller.AddMember(new AddProjectMemberDto
            {
                ProjectId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            });

            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        [Fact]
        public async Task RemoveMember_UnknownMember_ReturnsNotFound()
        {
            TestHelpers.SetUser(_controller, tenantId: Guid.NewGuid());
            _members.Setup(x => x.RemoveMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

            var result = await _controller.RemoveMember(Guid.NewGuid());

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Project member not found.", TestHelpers.ReadMessage(notFound.Value));
        }

        [Fact]
        public async Task RemoveMember_OwnTenantMember_ReturnsNoContent()
        {
            var tenantId = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            TestHelpers.SetUser(_controller, tenantId: tenantId);
            _members.Setup(x => x.RemoveMemberAsync(memberId, tenantId)).ReturnsAsync(true);

            var result = await _controller.RemoveMember(memberId);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
