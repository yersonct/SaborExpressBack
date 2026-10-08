using SaborExpress.Modules.UserPreferences.DTOs;

namespace SaborExpress.Modules.UserPreferences.Interfaces
{
    public interface IUserSettingsService
    {
        Task<UserSettingsResponseDto> GetAsync(int userId);
        Task<UserSettingsResponseDto> UpdateAsync(UpdateUserSettingsDto dto, int userId);
    }
}