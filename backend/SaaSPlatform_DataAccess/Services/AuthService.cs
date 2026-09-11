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

        public AuthService(IUnitOfWork unitOfWork, IConfiguration config, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _mapper = mapper;
        }

        public async Task<TokenResponseDto?> LoginAsync(LoginDto dto)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                return null;
            }

            // 🚫 Check Lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                throw new Exception($"Account is locked. Try again in {Math.Ceiling(user.LockoutEnd.Value.Subtract(DateTime.UtcNow).TotalMinutes)} minutes.");
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
                return null;
            }

            // Reset Lockout upon success
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            await _unitOfWork.Users.UpdateUser(user.Id, user);

            // Log successful authentication event
            await _unitOfWork.SystemLogs.LogAsync("LOGIN_SUCCESS", $"User {user.Email} successfully authenticated", user.Id, user.TenantId);

            var tokenResponse = GenerateTokensForUser(user);
            return tokenResponse;
        }

        public async Task<TokenResponseDto?> RegisterTenantAsync(RegisterTenantDto dto)
        {
            // Check if tenant email / domain / user email already exists
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(dto.AdminEmail);
            if (existingUser != null)
            {
                throw new Exception("Email address is already in use.");
            }

            // Fetch Subscription Plan from DB
            var plans = await _unitOfWork.SubscriptionPlans.GetAllAsync();
            var matchedPlan = System.Linq.Enumerable.FirstOrDefault(plans, p => p.Name.Equals(dto.Plan, StringComparison.OrdinalIgnoreCase));
            if (matchedPlan == null)
            {
                // Fallback to basic plan or create one if none exist
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

            // Simulate sending verification email in console
            Console.WriteLine($"[EMAIL SIMULATION] Verification Email sent to {user.Email} with token: {user.EmailVerificationToken}");

            // Generate Tokens
            var tokenResponse = GenerateTokensForUser(user);
            user.RefreshToken = tokenResponse.RefreshToken;
            user.RefreshTokenExpiryTime = tokenResponse.RefreshTokenExpiryTime;

            await _unitOfWork.Users.UpdateUser(user.Id, user);
            return tokenResponse;
        }

        public async Task<TokenResponseDto?> RefreshTokenAsync(string accessToken, string refreshToken)
        {
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

            var userId = Guid.Parse(userIdClaim);
            var user = await _unitOfWork.Users.GetUserById(userId);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
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
            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                return false; // Return false or simulate success to prevent user enumeration. We will return false.
            }

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiryTime = DateTime.UtcNow.AddHours(2); // Valid for 2 hours

            await _unitOfWork.Users.UpdateUser(user.Id, user);
            await _unitOfWork.SystemLogs.LogAsync("FORGOT_PASSWORD_REQUEST", $"Password reset request initiated for user: {user.Email}", user.Id, user.TenantId);

            // Simulate sending reset email in console
            Console.WriteLine($"[EMAIL SIMULATION] Password Reset Email sent to {user.Email} with token: {user.PasswordResetToken}");
            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
            if (user == null || user.PasswordResetToken != dto.Token || user.ResetTokenExpiryTime <= DateTime.UtcNow)
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.PasswordResetToken = null; // Clear token
            user.ResetTokenExpiryTime = null;
            user.FailedLoginAttempts = 0; // Clear attempts on password reset
            user.LockoutEnd = null;

            await _unitOfWork.Users.UpdateUser(user.Id, user);
            await _unitOfWork.SystemLogs.LogAsync("PASSWORD_RESET", $"Password reset completed for user: {user.Email}", user.Id, user.TenantId);
            return true;
        }

        private TokenResponseDto GenerateTokensForUser(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("TenantId", user.TenantId.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
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
                throw new SecurityTokenException("Invalid token signature.");
            }

            return principal;
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
