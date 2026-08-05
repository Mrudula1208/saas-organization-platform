using SaaSPlatform.Application.DTOS.Auth;
using SaaSPlatform.Application.DTOS.Users;
using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUser(Guid tenantId, string? search = null, string? role = null, bool? isActive = null);
        Task<User?> GetUserById(Guid Id);
        Task<User> CreateUser(User user);
        Task<bool> UpdateUser(Guid Id, User user);
        Task<bool> DeleteUser(Guid Id);
        Task<User> InviteUserAsync(Guid tenantId, InviteUserDto dto);
        Task<bool> ToggleUserStatusAsync(Guid Id, bool isActive);
    }
}
