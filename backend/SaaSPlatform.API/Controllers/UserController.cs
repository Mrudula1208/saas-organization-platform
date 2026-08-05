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
            var tenantClaim = User.FindFirst("TenantId")?.Value ?? HttpContext.Items["tenantId"]?.ToString();
            if (tenantClaim == null)
            {
                return Unauthorized(new { success = false, message = "Tenant ID is missing or unauthorized." });
            }

            var tenantId = Guid.Parse(tenantClaim);
            var users = await _userService.GetAllUser(tenantId, search, role, isActive);
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
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            try
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = dto.Name,
                    Email = dto.Email,
                    PasswordHash = dto.Password, // UserService will hash it
                    Role = "Member", // default employee role
                    TenantId = dto.TenantId,
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
        public async Task<ActionResult<User>> UpdateUser(Guid Id, [FromBody] User user)
        {
            var result = await _userService.UpdateUser(Id, user);
            if (!result)
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
                Message = "User updated successfully",
                Data = null
            });
        }

        [HttpDelete("{Id}")]
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
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
        {
            try
            {
                var tenantClaim = User.FindFirst("TenantId")?.Value ?? HttpContext.Items["tenantId"]?.ToString();
                if (tenantClaim == null)
                {
                    return Unauthorized(new { success = false, message = "Tenant ID is missing or unauthorized." });
                }

                var tenantId = Guid.Parse(tenantClaim);
                var invitedUser = await _userService.InviteUserAsync(tenantId, dto);
                
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
        public async Task<IActionResult> ToggleStatus(Guid Id, [FromBody] ToggleStatusRequest request)
        {
            var result = await _userService.ToggleUserStatusAsync(Id, request.IsActive);
            if (!result)
            {
                return NotFound(new { success = false, message = "User not found." });
            }
            return Ok(new { success = true, message = $"User status changed to {(request.IsActive ? "Active" : "Inactive")}." });
        }
    }

    public class ToggleStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
