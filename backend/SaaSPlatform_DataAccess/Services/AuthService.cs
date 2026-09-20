using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SaaSPlatform.Application.DTOS.Auth;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Model;
using SaaSPlatform_Model.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly IEmailService? _emailService;

        public AuthService(
            IUnitOfWork unitOfWork, 
            IConfiguration config, 
            IMapper mapper,
            IEmailService? emailService = null)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _mapper = mapper;
            _emailService = emailService;
        }

        public async Task<TokenResponseDto?> LoginAsync(LoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return null;
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                // Audit the failed attempt. The submitted password is never logged.
                await _unitOfWork.SystemLogs.LogAsync("LOGIN_FAILED", $"Failed login attempt for unknown email: {dto.Email}", null, null);
                return null;
            }

            // 🚫 Check Inactive or Deleted Account
            if (user.IsDeleted || !user.IsActive)
            {
                await _unitOfWork.SystemLogs.LogAsync("LOGIN_BLOCKED", $"Login blocked for inactive/deleted user: {user.Email}", user.Id, user.TenantId);
                throw new InvalidOperationException("Account is inactive or disabled. Please contact your organization administrator.");
            }

            // 🚫 Check Lockout
            if (user.LockoutEnd.HasValue)
            {
                if (user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    var minutes = Math.Max(1, (int)Math.Ceiling(user.LockoutEnd.Value.Subtract(DateTime.UtcNow).TotalMinutes));
                    throw new Exception($"Account is locked. Try again in {minutes} minutes.");
                }
                else
                {
                    // Lockout period has elapsed; reset attempts
                    user.LockoutEnd = null;
                    user.FailedLoginAttempts = 0;
                }
            }

            // 🔐 Verify Password
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    await _unitOfWork.SystemLogs.LogAsync("ACCOUNT_LOCKOUT", $"User account locked out due to multiple failed login attempts: {user.Email}", user.Id, user.TenantId);
                }
                await _unitOfWork.Users.UpdateUser(user.Id, user);
                // Audit the failed attempt. The submitted password is never logged.
                await _unitOfWork.SystemLogs.LogAsync("LOGIN_FAILED", $"Failed login attempt for user: {user.Email}", user.Id, user.TenantId);
                return null;
            }

            // Check maintenance mode (SuperAdmin users can still log in during maintenance)
            if (_unitOfWork.Settings != null)
            {
                var settings = await _unitOfWork.Settings.GetSettingsAsync();
                if (settings != null && settings.MaintenanceMode && !string.Equals(user.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    await _unitOfWork.SystemLogs.LogAsync("LOGIN_BLOCKED", $"Login blocked during maintenance mode for user: {user.Email}", user.Id, user.TenantId);
                    throw new InvalidOperationException("The platform is currently undergoing maintenance. Only Super Administrators can log in at this time. Please try again later.");
                }
            }

            // Reset Lockout upon success
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLogin = DateTime.UtcNow;

            var tokenResponse = GenerateTokensForUser(user);
            user.RefreshToken = tokenResponse.RefreshToken;
            user.RefreshTokenExpiryTime = tokenResponse.RefreshTokenExpiryTime;

            await _unitOfWork.Users.UpdateUser(user.Id, user);

            // Log successful authentication event
            await _unitOfWork.SystemLogs.LogAsync("LOGIN_SUCCESS", $"User {user.Email} successfully authenticated", user.Id, user.TenantId);

            return tokenResponse;
        }

        public async Task<TokenResponseDto?> RegisterTenantAsync(RegisterTenantDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Organization name is required.");
            if (string.IsNullOrWhiteSpace(dto.Domain))
                throw new ArgumentException("Domain name is required.");
            if (string.IsNullOrWhiteSpace(dto.AdminName))
                throw new ArgumentException("Admin full name is required.");
            if (string.IsNullOrWhiteSpace(dto.AdminEmail))
                throw new ArgumentException("A valid admin email address is required.");
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password is required.");
            if (dto.Password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters long.");
            if (!string.Equals(dto.Password, dto.ConfirmPassword))
                throw new ArgumentException("Passwords do not match.");

            // Check platform settings for maintenance mode and registration allowance
            if (_unitOfWork.Settings != null)
            {
                var settings = await _unitOfWork.Settings.GetSettingsAsync();
                if (settings != null)
                {
                    if (settings.MaintenanceMode)
                    {
                        throw new InvalidOperationException("New organization registrations are temporarily unavailable while the platform is in maintenance mode.");
                    }

                    if (!settings.AllowRegistrations)
                    {
                        throw new InvalidOperationException("Public organization registrations are currently disabled by system administrators.");
                    }
                }
            }

            // Check if tenant email / domain / user email already exists
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(dto.AdminEmail);
            if (existingUser != null)
            {
                throw new Exception("Email address is already in use.");
            }

            // Resolve the plan in SQL instead of loading every plan and
            // filtering the collection in memory.
            var matchedPlan = await _unitOfWork.SubscriptionPlans.GetByNameAsync(dto.Plan);
            if (matchedPlan == null)
            {
                // Compatibility path for older repository implementations.
                var plans = await _unitOfWork.SubscriptionPlans.GetAllAsync();
                matchedPlan = System.Linq.Enumerable.FirstOrDefault(plans, p => p.Name.Equals(dto.Plan, StringComparison.OrdinalIgnoreCase));
            }

            if (matchedPlan == null)
            {
                throw new Exception($"Selected subscription plan '{dto.Plan}' was not found in the database. Ensure seed data has run.");
            }

            // Create Tenant
            var tenantId = Guid.NewGuid();
            var tenant = new Tenant
            {
                Id = tenantId,
                Name = dto.Name,
                Domain = dto.Domain,
                ContactEmail = dto.AdminEmail,
                ContactPhone = string.Empty,
                SubscriptionPlanId = matchedPlan.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Tenants.AddAsync(tenant);

            // Create Admin User
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FullName = dto.AdminName,
                Email = dto.AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "TenantAdmin",
                TenantId = tenantId,
                IsActive = true,
                EmailVerificationToken = Guid.NewGuid().ToString(),
                EmailVerifiedAt = DateTime.UtcNow, // Verified by default for onboarding simplicity, or null for simulation
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            };

            await _unitOfWork.Users.CreateUser(user);

            // Write logs
            await _unitOfWork.SystemLogs.LogAsync("TENANT_CREATED", $"Tenant environment created: {tenant.Name} ({tenant.Domain})", userId, tenantId);
            await _unitOfWork.SystemLogs.LogAsync("USER_REGISTERED", $"Admin user registered: {user.Email}", userId, tenantId);

            // Save transaction
            await _unitOfWork.SaveChangesAsync();

            // Send welcome / verification email
            if (_emailService != null)
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, tenant.Name);
            }
            else
            {
                Console.WriteLine($"[EMAIL SIMULATION] Verification Email dispatched to {user.Email}.");
            }

            // Generate Tokens
            var tokenResponse = GenerateTokensForUser(user);
            user.RefreshToken = tokenResponse.RefreshToken;
            user.RefreshTokenExpiryTime = tokenResponse.RefreshTokenExpiryTime;

            await _unitOfWork.Users.UpdateUser(user.Id, user);
            return tokenResponse;
        }

        public async Task<TokenResponseDto?> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                return null;
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            var user = await _unitOfWork.Users.GetUserById(userId);

            if (user == null || user.IsDeleted || !user.IsActive ||
                string.IsNullOrEmpty(user.RefreshToken) ||
                user.RefreshToken != refreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null;
            }

            var tokenResponse = GenerateTokensForUser(user);
            user.RefreshToken = tokenResponse.RefreshToken;
            user.RefreshTokenExpiryTime = tokenResponse.RefreshTokenExpiryTime;

            await _unitOfWork.Users.UpdateUser(user.Id, user);
            await _unitOfWork.SystemLogs.LogAsync("TOKEN_REFRESH", $"Access token refreshed for user: {user.Email}", user.Id, user.TenantId);

            return tokenResponse;
        }

        public async Task<bool> VerifyEmailAsync(VerifyEmailDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Token))
            {
                return false;
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
            if (user == null || user.EmailVerificationToken != dto.Token)
            {
                return false;
            }

            user.EmailVerifiedAt = DateTime.UtcNow;
            user.EmailVerificationToken = null; // Clear token
            await _unitOfWork.Users.UpdateUser(user.Id, user);
            await _unitOfWork.SystemLogs.LogAsync("EMAIL_VERIFICATION", $"User email verified successfully: {user.Email}", user.Id, user.TenantId);
            return true;
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            {
                return false;
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                return false; // Return false to indicate no matching user (controller still returns generic OK message)
            }

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiryTime = DateTime.UtcNow.AddHours(2); // Valid for 2 hours

            await _unitOfWork.Users.UpdateUser(user.Id, user);
            await _unitOfWork.SystemLogs.LogAsync("FORGOT_PASSWORD_REQUEST", $"Password reset request initiated for user: {user.Email}", user.Id, user.TenantId);

            // Send password reset email
            if (_emailService != null)
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.PasswordResetToken);
            }
            else
            {
                Console.WriteLine($"[EMAIL SIMULATION] Password Reset Email dispatched to {user.Email}. Token: {user.PasswordResetToken}");
            }
            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return false;
            }

            if (!string.Equals(dto.Password, dto.ConfirmPassword))
            {
                throw new ArgumentException("Passwords do not match.");
            }

            if (dto.Password.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters long.");
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
            if (user == null || user.IsDeleted || !user.IsActive ||
                string.IsNullOrEmpty(user.PasswordResetToken) ||
                user.PasswordResetToken != dto.Token ||
                user.ResetTokenExpiryTime <= DateTime.UtcNow)
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.PasswordResetToken = null; // Clear token immediately to prevent reuse
            user.ResetTokenExpiryTime = null;
            user.FailedLoginAttempts = 0; // Clear attempts on password reset
            user.LockoutEnd = null;

            await _unitOfWork.Users.UpdateUser(user.Id, user);
            await _unitOfWork.SystemLogs.LogAsync("PASSWORD_RESET", $"Password reset completed for user: {user.Email}", user.Id, user.TenantId);
            return true;
        }

        public async Task LogoutAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetUserById(userId);
            if (user == null)
            {
                return;
            }

            // Revoke the refresh token so the session cannot be resumed.
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _unitOfWork.Users.UpdateUser(user.Id, user);

            await _unitOfWork.SystemLogs.LogAsync("LOGOUT", $"User {user.Email} logged out", user.Id, user.TenantId);
        }

        private TokenResponseDto GenerateTokensForUser(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("TenantId", user.TenantId.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Role", user.Role),
                new Claim(ClaimTypes.Name, user.FullName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60), // Access token valid for 60 minutes
                signingCredentials: creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            // Generate cryptographically secure random refresh token
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7), // Refresh token valid for 7 days
                Email = user.Email,
                Role = user.Role,
                TenantId = user.TenantId,
                FullName = user.FullName
            };
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"])),
                    ValidateLifetime = false // Here we map key details from expired token
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }

    public static class MathExtension
    {
        public static int CeRounding(double value)
        {
            return (int)Math.Ceiling(value);
        }
    }
}
