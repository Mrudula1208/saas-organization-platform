using SaaSPlatform.Application.DTOS.ProjectMembers;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class ProjectMemberService : IProjectMemberService
    {
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISystemLogRepository _systemLogs;

        public ProjectMemberService(
            IProjectMemberRepository projectMemberRepository,
            IProjectRepository projectRepository,
            IUserRepository userRepository,
            ISystemLogRepository systemLogs)
        {
            _projectMemberRepository = projectMemberRepository;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _systemLogs = systemLogs;
        }

        public async Task<IEnumerable<ProjectMemberDto>> GetMembersByProjectAsync(Guid projectId, Guid tenantId)
        {
            var project = await _projectRepository.GetByIdAsync(projectId);
            if (project == null || project.IsDeleted || project.TenantId != tenantId)
                return Enumerable.Empty<ProjectMemberDto>();

            var members = await _projectMemberRepository.GetMembersByProjectAsync(projectId, tenantId);
            return members.Select(MapToDto).ToList();
        }

        public async Task<ProjectMemberDto> AddMemberAsync(AddProjectMemberDto dto, Guid tenantId)
        {
            var project = await _projectRepository.GetByIdAsync(dto.ProjectId);
            if (project == null || project.IsDeleted || project.TenantId != tenantId)
                throw new KeyNotFoundException("Project not found in this tenant.");

            var user = await _userRepository.GetUserById(dto.UserId);
            if (user == null || user.IsDeleted || user.TenantId != tenantId)
                throw new KeyNotFoundException("User not found in this tenant.");

            var member = await _projectMemberRepository.AddAsync(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                UserId = dto.UserId
            });

            await _systemLogs.LogAsync("PROJECT_MEMBER_ADDED", $"User {user.Email} added to project {project.Name}.", user.Id, tenantId);

            return MapToDto(member);
        }

        public async Task<bool> RemoveMemberAsync(Guid memberId, Guid tenantId)
        {
            var member = await _projectMemberRepository.GetByIdAsync(memberId);
            if (member == null || member.Project == null || member.Project.TenantId != tenantId)
                return false;

            await _projectMemberRepository.DeleteAsync(member);
            await _systemLogs.LogAsync("PROJECT_MEMBER_REMOVED", $"User removed from project {member.Project.Name}.", member.UserId, tenantId);
            return true;
        }

        private static ProjectMemberDto MapToDto(ProjectMember member)
        {
            return new ProjectMemberDto
            {
                Id = member.Id,
                ProjectId = member.ProjectId,
                ProjectName = member.Project?.Name ?? string.Empty,
                UserId = member.UserId,
                UserFullName = member.User?.FullName ?? string.Empty,
                UserEmail = member.User?.Email ?? string.Empty,
                UserRole = member.User?.Role ?? string.Empty,
                UserProfileImageUrl = member.User?.ProfileImageUrl
            };
        }
    }
}