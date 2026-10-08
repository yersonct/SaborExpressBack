using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IEmailConfirmationRepository
    {
        Task AddAsync(EmailConfirmationCode code);
        Task<EmailConfirmationCode?> GetLatestCodeByUserIdAsync(int userId);
        Task InvalidatePendingCodesAsync(int userId);
        Task UpdateAsync(EmailConfirmationCode code);
        Task<EmailConfirmationCode?> GetByTokenAsync(string token);
    }
}
