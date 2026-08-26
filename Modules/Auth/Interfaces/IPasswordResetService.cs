using System.Threading.Tasks;
using SaborExpress.Modules.Auth.DTOs;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IPasswordResetService
    {
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<VerifyResetCodeResponseDto> VerifyResetCodeAsync(VerifyResetCodeDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
    }
}
