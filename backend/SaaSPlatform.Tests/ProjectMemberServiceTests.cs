using Moq;
using SaaSPlatform.Application.DTOS.ProjectMembers;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Application.Services;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Model;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class ProjectMemberServiceTests
    {
        private readonly Mock<IProjectMemberRepository> _members = new();
        private readonly Mock<IProjectRepository> _projects = new();
        private readonly Mock<IUserRepository> _users = new();
        private readonly Mock<ISystemLogRepository> _logs = new();
        private readonly ProjectMemberService _service;

        public ProjectMemberServiceTests()
        {
            _service = new ProjectMemberService(_members.Object, _projects.Object, _users.Object, _logs.Object);
        }

        private static Project CreateProject(Guid tenantId, string name = "Website")
        {
            return new Project
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = "",
                TenantId = tenantId,
                OwnerId = Guid.NewGuid(),
                Status = "Active",
                Priority = "Medium",
                IsActive = true,
                IsDeleted = false
            };
        }

        private static User CreateUser(Guid tenantId)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                FullName = "Jane Member",
                Email = "jane@acme.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                Role = "Member",
                TenantId = tenantId,
                IsActive = true
            };
        }

        [Fact]
        public async Task GetMembersByProject_ProjectFromAnotherTenant_ReturnsEmpty()
        {
            var project = CreateProject(Guid.NewGuid());
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            var result = (await _service.GetMembersByProjectAsync(project.Id, Guid.NewGuid())).ToList();

            Assert.Empty(result);
            _members.Verify(
                x => x.GetMembersByProjectAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetMembersByProject_OwnProject_ReturnsMappedMembers()
        {
            var tenantId = Guid.NewGuid();
            var project = CreateProject(tenantId);
            var user = CreateUser(tenantId);
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);
            _members.Setup(x => x.GetMembersByProjectAsync(project.Id, tenantId))
                .ReturnsAsync(new List<ProjectMember>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = project.Id,
                        UserId = user.Id,
                        Project = project,
                        User = user
                    }
                });

            var result = (await _service.GetMembersByProjectAsync(project.Id, tenantId)).ToList();

            var member = Assert.Single(result);
            Assert.Equal("Website", member.ProjectName);
            Assert.Equal("Jane Member", member.UserFullName);
        }

        [Fact]
        public async Task AddMember_UnknownProject_ThrowsKeyNotFound()
        {
            _projects.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Project?)null);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AddMemberAsync(new AddProjectMemberDto
                {
                    ProjectId = Guid.NewGuid(),
                    UserId = Guid.NewGuid()
                }, Guid.NewGuid()));

            Assert.Equal("Project not found in this tenant.", ex.Message);
        }

        [Fact]
        public async Task AddMember_ProjectFromAnotherTenant_ThrowsKeyNotFound()
        {
            var project = CreateProject(Guid.NewGuid()); // tenant B
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AddMemberAsync(new AddProjectMemberDto
                {
                    ProjectId = project.Id,
                    UserId = Guid.NewGuid()
                }, Guid.NewGuid())); // tenant A

            _members.Verify(x => x.AddAsync(It.IsAny<ProjectMember>()), Times.Never);
        }

        [Fact]
        public async Task AddMember_UserFromAnotherTenant_ThrowsKeyNotFound()
        {
            var tenantId = Guid.NewGuid();
            var project = CreateProject(tenantId);
            var foreignUser = CreateUser(Guid.NewGuid()); // tenant B
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);
            _users.Setup(x => x.GetUserById(foreignUser.Id)).ReturnsAsync(foreignUser);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AddMemberAsync(new AddProjectMemberDto
                {
                    ProjectId = project.Id,
                    UserId = foreignUser.Id
                }, tenantId));

            Assert.Equal("User not found in this tenant.", ex.Message);
            _members.Verify(x => x.AddAsync(It.IsAny<ProjectMember>()), Times.Never);
        }

        [Fact]
        public async Task AddMember_ValidUser_AddsMemberAndAudits()
        {
            var tenantId = Guid.NewGuid();
            var project = CreateProject(tenantId);
            var user = CreateUser(tenantId);
            _projects.Setup(x => x.GetByIdAsync(project.Id)).ReturnsAsync(project);
            _users.Setup(x => x.GetUserById(user.Id)).ReturnsAsync(user);
            _members.Setup(x => x.AddAsync(It.IsAny<ProjectMember>()))
                .ReturnsAsync((ProjectMember m) => m);

            var member = await _service.AddMemberAsync(new AddProjectMemberDto
            {
                ProjectId = project.Id,
                UserId = user.Id
            }, tenantId);

            Assert.Equal(project.Id, member.ProjectId);
            Assert.Equal(user.Id, member.UserId);
            _logs.Verify(
                x => x.LogAsync("PROJECT_MEMBER_ADDED", It.IsAny<string>(), user.Id, tenantId),
                Times.Once);
        }

        [Fact]
        public async Task RemoveMember_MemberFromAnotherTenant_ReturnsFalse()
        {
            var foreignProject = CreateProject(Guid.NewGuid()); // tenant B
            var member = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = foreignProject.Id,
                UserId = Guid.NewGuid(),
                Project = foreignProject
            };
            _members.Setup(x => x.GetByIdAsync(member.Id)).ReturnsAsync(member);

            var result = await _service.RemoveMemberAsync(member.Id, Guid.NewGuid()); // tenant A

            Assert.False(result);
            _members.Verify(x => x.DeleteAsync(It.IsAny<ProjectMember>()), Times.Never);
        }

        [Fact]
        public async Task RemoveMember_OwnTenantMember_RemovesAndAudits()
        {
            var tenantId = Guid.NewGuid();
            var project = CreateProject(tenantId);
            var member = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = Guid.NewGuid(),
                Project = project
            };
            _members.Setup(x => x.GetByIdAsync(member.Id)).ReturnsAsync(member);
            _members.Setup(x => x.DeleteAsync(member)).Returns(Task.CompletedTask);

            var result = await _service.RemoveMemberAsync(member.Id, tenantId);

            Assert.True(result);
            _members.Verify(x => x.DeleteAsync(member), Times.Once);
            _logs.Verify(
                x => x.LogAsync("PROJECT_MEMBER_REMOVED", It.IsAny<string>(), member.UserId, tenantId),
                Times.Once);
        }
    }
}
