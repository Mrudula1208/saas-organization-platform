using SaaSPlatform.Application.DTOS;
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
        private readonly ITenantRepository? _tenantRepository;
        private readonly ISubscriptionPlanRepository? _planRepository;

        public UserService(
            IUserRepository userRepository,
            ISystemLogRepository systemLogs,
            ITenantRepository? tenantRepository = null,
            ISubscriptionPlanRepository? planRepository = null)
        {
            _userRepository = userRepository;
            _systemLogs = systemLogs;
            _tenantRepository = tenantRepository;
            _planRepository = planRepository;
        }

        // One page of the tenant user list; the database does the filtering and paging.
        public async Task<PagedResult<User>> GetUsersPage(Guid tenantId, string? search = null, string? role = null, bool? isActive = null, int page = 1, int pageSize = 20)
        {
            return await _userRepository.GetUsersPage(tenantId, search, role, isActive, page, pageSize);
        }

        // One page of the super admin list: null tenant id means no tenant filter.
        public async Task<PagedResult<User>> GetPlatformUsersPage(string? search = null, string? role = null, bool? isActive = null, int page = 1, int pageSize = 20)
        {
            return await _userRepository.GetUsersPage(null, search, role, isActive, page, pageSize);
        }

        public async Task<User?> GetUserById(Guid Id)
        {
            var user = await _userRepository.GetUserById(Id);
            if (user == null || user.IsDeleted) return null;
            return user;
        }

        public async Task<User> CreateUser(User user)
        {
            // Verify tenant user limits before creation
            if (user.TenantId != Guid.Empty)
            {
                await CheckUserLimitAsync(user.TenantId);
            }

            // Hash password if not already hashed
            if (!string.IsNullOrEmpty(user.PasswordHash) && !user.PasswordHash.StartsWith("$2b$") && !user.PasswordHash.StartsWith("$2a$"))
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
            await CheckUserLimitAsync(tenantId);

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

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            // Defense in depth: never trust the caller, confirmation must match.
            if (!string.Equals(dto.NewPassword, dto.ConfirmPassword, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("New password and confirmation do not match.");
            }

            var user = await _userRepository.GetUserById(userId);
            if (user == null || user.IsDeleted)
            {
                return false;
            }

            // Verify the current password before allowing the change.
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }

            // Never store plaintext: always hash with BCrypt before persisting.
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            var result = await _userRepository.UpdateUser(userId, user);
            if (result)
            {
                await _systemLogs.LogAsync("PASSWORD_CHANGED", $"User {user.Email} changed their password.", user.Id, user.TenantId);
            }
            return result;
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null || user.IsDeleted)
            {
                return false;
            }

            // Only allowed profile fields are updated. Email, role and password are never touched here.
            user.FullName = dto.FullName.Trim();
            user.ProfileImageUrl = dto.ProfileImageUrl?.Trim();

            var result = await _userRepository.UpdateUser(userId, user);
            if (result)
            {
                await _systemLogs.LogAsync("PROFILE_UPDATED", $"User {user.Email} updated their profile.", user.Id, user.TenantId);
            }
            return result;
        }

        public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null || user.IsDeleted)
            {
                return null;
            }

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                TenantId = user.TenantId,
                ProfileImageUrl = user.ProfileImageUrl,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            };
        }

        private async Task CheckUserLimitAsync(Guid tenantId)
        {
            if (_tenantRepository == null || _planRepository == null || tenantId == Guid.Empty)
            {
                return;
            }

            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null || tenant.SubscriptionPlanId == Guid.Empty)
            {
                return;
            }

            var plan = await _planRepository.GetByIdAsync(tenant.SubscriptionPlanId);
            if (plan == null || plan.MaxUsers <= 0)
            {
                return;
            }

            var currentUsers = await _userRepository.CountActiveUsersByTenantAsync(tenantId);
            if (currentUsers >= plan.MaxUsers)
            {
                throw new InvalidOperationException($"This organization has reached the limit of {plan.MaxUsers} user(s) allowed on the {plan.Name} plan. Please upgrade your subscription to add more members.");
            }
        }
    }
}
