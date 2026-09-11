using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.DTOS.Tasks
{
    public class UpdateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Guid AssignedUserId { get; set; }

        public string Status { get; set; }

        public string Priority { get; set; }

        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; }
    }
}
