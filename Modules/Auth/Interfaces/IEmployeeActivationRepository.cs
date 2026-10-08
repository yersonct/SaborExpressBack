// Modules/Auth/Interfaces/IEmployeeActivationRepository.cs
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IEmployeeActivationRepository
    {
        Task<EmployeeActivationCode?> GetLatestByUserIdAsync(int userId);
        Task AddAsync(EmployeeActivationCode code);
        Task UpdateAsync(EmployeeActivationCode code);
        Task<EmployeeActivationCode?> GetByTokenAsync(string token);
        Task InvalidatePendingAsync(int userId);
        Task SaveChangesAsync();
    }
}
