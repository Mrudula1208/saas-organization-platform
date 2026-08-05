using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.Auth;
using SaaSPlatform.Application.Interfaces;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                if (result == null)
                {
                    return Unauthorized(new { success = false, message = "Invalid Email or Password" });
                }
                return Ok(new { success = true, data = result });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("register-tenant")]
        public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantDto dto)
        {
            try
            {
                var result = await _authService.RegisterTenantAsync(dto);
                if (result == null)
                {
                    return BadRequest(new { success = false, message = "Registration failed." });
                }
                return Ok(new { success = true, data = result });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRequestDto dto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(dto.AccessToken, dto.RefreshToken);
                if (result == null)
                {
                    return BadRequest(new { success = false, message = "Invalid access token or refresh token." });
                }
                return Ok(new { success = true, data = result });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
        {
            var result = await _authService.VerifyEmailAsync(dto);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Invalid verification token or email." });
            }
            return Ok(new { success = true, message = "Email verified successfully." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);
            // Always return Ok to prevent user enumeration attacks
            return Ok(new { success = true, message = "If the email exists, a password reset link has been dispatched." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to reset password. Check details or token expiry." });
            }
            return Ok(new { success = true, message = "Password reset successfully." });
        }
    }

    public class TokenRequestDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
