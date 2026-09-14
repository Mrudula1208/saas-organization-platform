using SaaSPlatform.Application.DTOS;
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
        // One page of the tenant user list. Filtering and paging happen in the database.
        Task<PagedResult<User>> GetUsersPage(Guid tenantId, string? search = null, string? role = null, bool? isActive = null, int page = 1, int pageSize = 20);

        // One page of the super admin list (users of every tenant).
        Task<PagedResult<User>> GetPlatformUsersPage(string? search = null, string? role = null, bool? isActive = null, int page = 1, int pageSize = 20);
        Task<User?> GetUserById(Guid Id);
        Task<User> CreateUser(User user);
        Task<bool> UpdateUser(Guid Id, User user);
        Task<bool> DeleteUser(Guid Id);
        Task<User> InviteUserAsync(Guid tenantId, InviteUserDto dto);
        Task<bool> ToggleUserStatusAsync(Guid Id, bool isActive);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task<UserProfileDto?> GetProfileAsync(Guid userId);
    }
}
