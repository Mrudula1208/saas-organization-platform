using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Infrastructure.Repositories
{
    public interface IProjectRepository
    {
        Task<IEnumerable<Project>> GetAllAsync(Guid tenantId);

        // Get single project by ID
        Task<Project?> GetByIdAsync(Guid Id);

        // Add new project
        Task<Project> AddAsync(Project project);

        // Update project
        Task UpdateAsync(Project project);

        // Delete project
        Task DeleteAsync(Project project);
    }
}
