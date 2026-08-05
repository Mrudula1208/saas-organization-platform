using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllUsers(Guid tenantId);

        Task<User?>GetUserById(Guid Id);

        Task<User> CreateUser(User user);   

        Task<bool> UpdateUser(Guid Id ,User user);
        Task <bool>DeleteUser(Guid Id);
        Task<User?>GetByEmailAsync (string  Email);


    }
}
