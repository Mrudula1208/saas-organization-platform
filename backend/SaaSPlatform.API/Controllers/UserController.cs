using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS;
using SaaSPlatform.Application.DTOS.Users;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Model;
using SaaSPlatform_Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        // The only roles that exist in this application.
        private static readonly string[] KnownRoles = { "SuperAdmin", "TenantAdmin", "Member" };

        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<User>>> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            // Super admins manage users of every tenant; every other role only sees its own tenant.
            var users = IsSuperAdmin()
                ? await _userService.GetPlatformUsersPage(search, role, isActive, page, pageSize)
                : await _userService.GetUsersPage(tenantId.Value, search, role, isActive, page, pageSize);
            return Ok(users);
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<User>> GetUserById(Guid Id)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var user = await _userService.GetUserById(Id);
            if (user == null)
            {
                return NotFound(new ApiResponse<User>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null
                });
            }

            // A user can only be read from inside the caller's tenant (super admins may read any user).
            if (user.TenantId != tenantId.Value && !IsSuperAdmin())
            {
                return Forbid();
            }

            return Ok(new ApiResponse<User>
            {
                Success = true,
                Message = "User fetched successfully",
                Data = user
            });
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            try
            {
                var tenantId = GetTenantId();
                if (tenantId == null) return Unauthorized();

                var role = string.IsNullOrWhiteSpace(dto.Role) ? "Member" : dto.Role;
                if (!CanAssignRole(role))
                {
                    return BadRequest(new { success = false, message = "Selected role is not allowed." });
                }

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = dto.Name,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Role = role,
                    TenantId = tenantId.Value,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow,
                    ProfileImageUrl = string.Empty
                };

                var createdUser = await _userService.CreateUser(user);

                return Ok(new ApiResponse<User>
                {
                    Success = true,
                    Message = "User created successfully",
                    Data = createdUser
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{Id}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<ActionResult<User>> UpdateUser(Guid Id, [FromBody] UpdateUserDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var existingUser = await _userService.GetUserById(Id);
            if (existingUser == null)
            {
                return NotFound(new ApiResponse<User>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null
                });
            }

            if (existingUser.TenantId != tenantId.Value && !IsSuperAdmin())
            {
                return Forbid();
            }

            // No tenant admin can ever grant the SuperAdmin role, and only known roles are accepted.
            if (dto.Role == "SuperAdmin" && !IsSuperAdmin())
            {
                return Forbid();
            }
            if (!KnownRoles.Contains(dto.Role))
            {
                return BadRequest(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Invalid role.",
                    Data = null
                });
            }

            var user = new User
            {
                FullName = dto.Name,
                Email = dto.Email,
                Role = dto.Role,
                ProfileImageUrl = dto.ProfileImageUrl ?? string.Empty
            };

            var result = await _userService.UpdateUser(Id, user);
            if (!result)
            {
                return BadRequest(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Update failed",
                    Data = null
                });
            }

            // The edit form can change the active status together with the other fields.
            if (dto.IsActive.HasValue)
            {
                await _userService.ToggleUserStatusAsync(Id, dto.IsActive.Value);
            }

            return Ok(new ApiResponse<User>
            {
                Success = true,
                Message = "User updated successfully",
                Data = null
            });
        }

        [HttpDelete("{Id}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<ActionResult> DeleteUser(Guid Id)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var existingUser = await _userService.GetUserById(Id);
            if (existingUser == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null
                });
            }

            // A user can only be deleted inside the caller's tenant (super admins may delete any user).
            if (existingUser.TenantId != tenantId.Value && !IsSuperAdmin())
            {
                return Forbid();
            }

            var result = await _userService.DeleteUser(Id);
            if (!result)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null
                });
            }

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "User deleted successfully",
                Data = null
            });
        }

        [HttpPost("invite")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
        {
            try
            {
                var tenantId = GetTenantId();
                if (tenantId == null) return Unauthorized();

                var role = string.IsNullOrWhiteSpace(dto.Role) ? "Member" : dto.Role;
                if (!CanAssignRole(role))
                {
                    return BadRequest(new { success = false, message = "Selected role is not allowed." });
                }
                dto.Role = role;

                var invitedUser = await _userService.InviteUserAsync(tenantId.Value, dto);
                invitedUser.PasswordHash = string.Empty;
                invitedUser.Tenant = null;
                invitedUser.AssignedTasks = null;

                return Ok(new ApiResponse<User>
                {
                    Success = true,
                    Message = "User invited successfully. Verification email logged.",
                    Data = invitedUser
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{Id}/toggle-status")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> ToggleStatus(Guid Id, [FromBody] ToggleStatusRequest request)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var existingUser = await _userService.GetUserById(Id);
            if (existingUser == null)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            // A status can only be changed inside the caller's tenant (super admins may change any user).
            if (existingUser.TenantId != tenantId.Value && !IsSuperAdmin())
            {
                return Forbid();
            }

            var result = await _userService.ToggleUserStatusAsync(Id, request.IsActive);
            if (!result)
            {
                return NotFound(new { success = false, message = "User not found." });
            }
            return Ok(new { success = true, message = $"User status changed to {(request.IsActive ? "Active" : "Inactive")}." });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // User identity always comes from the JWT claims of the authenticated request.
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var profile = await _userService.GetProfileAsync(userId.Value);
            if (profile == null)
            {
                return NotFound(new ApiResponse<UserProfileDto>
                {
                    Success = false,
                    Message = "User not found.",
                    Data = null
                });
            }

            return Ok(new ApiResponse<UserProfileDto>
            {
                Success = true,
                Message = "Profile fetched successfully.",
                Data = profile
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _userService.UpdateProfileAsync(userId.Value, dto);
            if (!result)
            {
                return BadRequest(new ApiResponse<UserProfileDto>
                {
                    Success = false,
                    Message = "Failed to update profile. User not found.",
                    Data = null
                });
            }

            var profile = await _userService.GetProfileAsync(userId.Value);
            return Ok(new ApiResponse<UserProfileDto>
            {
                Success = true,
                Message = "Profile updated successfully.",
                Data = profile
            });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            // Target user is always the authenticated user from JWT claims.
            // There is no user id parameter, so a user can never change another user's password here.
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var result = await _userService.ChangePasswordAsync(userId.Value, dto);
                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found.",
                        Data = null
                    });
                }
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Password changed successfully.",
                    Data = null
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        private Guid? GetTenantId()
        {
            var tenantClaim = User.FindFirst("TenantId")?.Value;
            if (tenantClaim != null && Guid.TryParse(tenantClaim, out var tenantId) && tenantId != Guid.Empty)
                return tenantId;
            return null;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) && userId != Guid.Empty)
                return userId;
            return null;
        }

        private bool IsSuperAdmin()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("Role")?.Value;
            return role == "SuperAdmin";
        }

        // Only a super admin can hand out the SuperAdmin role; all callers are limited to known roles.
        private bool CanAssignRole(string role)
        {
            if (!KnownRoles.Contains(role)) return false;
            if (role == "SuperAdmin" && !IsSuperAdmin()) return false;
            return true;
        }
    }

    public class ToggleStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
