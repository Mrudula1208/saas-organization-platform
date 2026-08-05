using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.DTOS.Tasks
{
    public class CreateTaskDto
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Guid ProjectId { get; set; }

        public Guid AssignedUserId { get; set; }

        public string Status { get; set; } = "ToDo";

        public string Priority { get; set; } = "Medium";

        public DateTime DueDate { get; set; }
    }
}
