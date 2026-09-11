using SaaSPlatform.Application.DTOS.ProjectMembers;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class ProjectMemberService:IProjectMemberService
    {
        private readonly IProjectMemberRepository _projectrepository;

        public ProjectMemberService (IProjectMemberRepository projectrepository)
        {
            _projectrepository = projectrepository;

        }


        public async Task<IEnumerable<ProjectMember>> GetMemberAsync(Guid tenantId)
        {

            return await _projectrepository.GetMembersAsync(tenantId);

        }


        public async Task<ProjectMember> AddMemberAsync(AddProjectMemberDto dto)
        {
            var member = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                UserId = dto.UserId,

            };
            return await _projectrepository.AddAsync(member);

        }
            public async Task RemoveAsync(Guid Id)
        {
            var member = await _projectrepository.GetByIdAsync(Id);
            {
                if (member == null)
                {
                    throw new Exception("Member not found");
                }
                 await _projectrepository.DeleteAsync(member);
            }
        }
        
        
        
        

    }


}
