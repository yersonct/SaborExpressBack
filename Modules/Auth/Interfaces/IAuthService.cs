using SaborExpress.Modules.Auth.DTOs;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task LogoutAsync(int userId);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
    }
}
