using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IEmailConfirmationService
    {
        Task SendConfirmationCodeAsync(User user);
        Task ConfirmEmailAsync(ConfirmEmailDto dto);
        Task ResendConfirmationCodeAsync(ResendConfirmationDto dto);
    }
}
