using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken token);
        Task<RefreshToken?> GetActiveByTokenAsync(string token);
        Task RevokeAsync(RefreshToken token);
        Task RevokeAllForUserAsync(int userId);
    }
}
