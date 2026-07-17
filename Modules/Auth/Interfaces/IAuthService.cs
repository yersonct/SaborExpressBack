using SaborExpress.Modules.Auth.DTOs;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }
}