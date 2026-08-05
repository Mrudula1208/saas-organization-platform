using SaaSPlatform.Application.DTOS.Users;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISystemLogRepository _systemLogs;

        public UserService(IUserRepository userRepository, ISystemLogRepository systemLogs)
        {
            _userRepository = userRepository;
            _systemLogs = systemLogs;
        }

        public async Task<IEnumerable<User>> GetAllUser(Guid tenantId, string? search = null, string? role = null, bool? isActive = null)
        {
            var users = await _userRepository.GetAllUsers(tenantId);
            var query = users.AsQueryable();

            // Filter out soft-deleted users
            query = query.Where(u => !u.IsDeleted);

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(lowerSearch) || u.Email.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            return query.ToList();
        }

        public async Task<User?> GetUserById(Guid Id)
        {
            var user = await _userRepository.GetUserById(Id);
            if (user == null || user.IsDeleted) return null;
            return user;
        }

        public async Task<User> CreateUser(User user)
        {
            // Hash password if not already hashed
            if (!user.PasswordHash.StartsWith("$2b$") && !user.PasswordHash.StartsWith("$2a$"))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            }

            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;
            user.IsDeleted = false;

            var createdUser = await _userRepository.CreateUser(user);
            await _systemLogs.LogAsync("USER_CREATED", $"User {createdUser.Email} created in system.", createdUser.Id, createdUser.TenantId);
            return createdUser;
        }

        public async Task<bool> UpdateUser(Guid Id, User user)
        {
            var existingUser = await _userRepository.GetUserById(Id);
            if (existingUser == null || existingUser.IsDeleted)
            {
                return false;
            }

            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;
            existingUser.ProfileImageUrl = user.ProfileImageUrl;

            if (!string.IsNullOrEmpty(user.PasswordHash) && !user.PasswordHash.StartsWith("$2b$") && !user.PasswordHash.StartsWith("$2a$"))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            }

            var result = await _userRepository.UpdateUser(Id, existingUser);
            if (result)
            {
                await _systemLogs.LogAsync("USER_UPDATED", $"User {existingUser.Email} updated.", existingUser.Id, existingUser.TenantId);
            }
            return result;
        }

        public async Task<bool> DeleteUser(Guid Id)
        {
            var existingUser = await _userRepository.GetUserById(Id);
            if (existingUser == null || existingUser.IsDeleted)
            {
                return false;
            }

            // Perform Soft Delete
            existingUser.IsDeleted = true;
            var result = await _userRepository.UpdateUser(Id, existingUser);
            if (result)
            {
                await _systemLogs.LogAsync("USER_DELETED", $"User {existingUser.Email} soft deleted.", existingUser.Id, existingUser.TenantId);
            }
            return result;
        }

        public async Task<User> InviteUserAsync(Guid tenantId, InviteUserDto dto)
        {
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new Exception("Email is already registered.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = dto.Email,
                Role = dto.Role,
                TenantId = tenantId,
                IsActive = false, // starts inactive until activated
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // random temp password
                EmailVerificationToken = Guid.NewGuid().ToString(), // Invite token
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.CreateUser(user);
            await _systemLogs.LogAsync("USER_INVITED", $"User {createdUser.Email} invited to join tenant.", createdUser.Id, tenantId);

            // Simulate sending invitation email
            Console.WriteLine($"[EMAIL SIMULATION] Invite User sent to {createdUser.Email} with token {createdUser.EmailVerificationToken}");

            return createdUser;
        }

        public async Task<bool> ToggleUserStatusAsync(Guid Id, bool isActive)
        {
            var user = await _userRepository.GetUserById(Id);
            if (user == null || user.IsDeleted)
            {
                return false;
            }

            user.IsActive = isActive;
            var result = await _userRepository.UpdateUser(Id, user);
            if (result)
            {
                var action = isActive ? "USER_ACTIVATED" : "USER_DEACTIVATED";
                await _systemLogs.LogAsync(action, $"User {user.Email} status changed to {(isActive ? "Active" : "Inactive")}.", user.Id, user.TenantId);
            }
            return result;
        }
    }
}
