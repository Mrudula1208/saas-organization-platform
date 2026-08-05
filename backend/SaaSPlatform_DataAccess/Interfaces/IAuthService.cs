using SaaSPlatform.Application.DTOS.Auth;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.Interfaces
{
    public interface IAuthService
    {
        Task<TokenResponseDto?> LoginAsync(LoginDto dto);
        Task<TokenResponseDto?> RegisterTenantAsync(RegisterTenantDto dto);
        Task<TokenResponseDto?> RefreshTokenAsync(string accessToken, string refreshToken);
        Task<bool> VerifyEmailAsync(VerifyEmailDto dto);
        Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
