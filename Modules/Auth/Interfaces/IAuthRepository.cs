using SaborExpress.Modules.Auth.Models;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> ExistsByEmailAsync(string email);
        Task<User?> GetByIdentifierAsync(string email);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task<User?> GetByIdWithRelationsAsync(int id);
        Task<bool> ExistsByEmailAsync(string email, int excludeUserId);
    }
}
