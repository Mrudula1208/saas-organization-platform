using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.Users;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Model;
using SaaSPlatform_Utility;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var users = await _userService.GetAllUser(tenantId.Value, search, role, isActive);
            return Ok(users);
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<User>> GetUserById(Guid Id)
        {
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

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = dto.Name,
                    Email = dto.Email,
                    PasswordHash = dto.Password,
                    Role = "Member",
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

            var tenantId = GetTenantId();
            if (tenantId == null || existingUser.TenantId != tenantId.Value)
            {
                return Forbid();
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

                var invitedUser = await _userService.InviteUserAsync(tenantId.Value, dto);
                
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
            var result = await _userService.ToggleUserStatusAsync(Id, request.IsActive);
            if (!result)
            {
                return NotFound(new { success = false, message = "User not found." });
            }
            return Ok(new { success = true, message = $"User status changed to {(request.IsActive ? "Active" : "Inactive")}." });
        }

        private Guid? GetTenantId()
        {
            var tenantClaim = User.FindFirst("TenantId")?.Value;
            if (tenantClaim != null && Guid.TryParse(tenantClaim, out var tenantId) && tenantId != Guid.Empty)
                return tenantId;
            return null;
        }
    }

    public class ToggleStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
