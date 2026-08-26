// Modules/Auth/Interfaces/IEmployeeActivationService.cs
using SaborExpress.Modules.Auth.DTOs;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IEmployeeActivationService
    {
        Task SendActivationCodeAsync(int userId, string email, string employeeName);
        Task ActivateAccountAsync(ActivateAccountDto dto);
    }
}
