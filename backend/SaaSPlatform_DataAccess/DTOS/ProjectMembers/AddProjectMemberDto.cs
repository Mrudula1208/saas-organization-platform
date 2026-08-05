using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.DTOS.ProjectMembers
{
    public class AddProjectMemberDto
    {
        // Used when client sends request to add user into project

        public Guid ProjectId { get; set; }
        public Guid  UserId { get; set; }
    }
}
