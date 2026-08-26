using System.Threading.Tasks;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IPasswordResetRepository
    {
        Task AddAsync(PasswordResetCode resetCode);
        Task<PasswordResetCode?> GetActiveCodeAsync(int userId, string code);
        Task<PasswordResetCode?> GetByResetTokenAsync(string resetToken);
        Task UpdateAsync(PasswordResetCode resetCode);
        Task InvalidatePendingCodesAsync(int userId);
        Task<PasswordResetCode?> GetLatestCodeByUserIdAsync(int userId);
        Task<List<DateTime>> GetRecentAttemptTimestampsAsync(int userId, DateTime since);
    }
}
