// Modules/UserPreferences/Interfaces/IUserPreferenceService.cs
using SaborExpress.Modules.UserPreferences.DTOs;

namespace SaborExpress.Modules.UserPreferences.Interfaces
{
    public interface IUserPreferenceService
    {
        Task<List<UserPreferenceResponseDto>> GetByUserIdAsync(int userId);
        Task<UserPreferenceResponseDto> CreateAsync(CreateUserPreferenceDto dto, int userId);
        Task<UserPreferenceResponseDto> UpdateAsync(int id, UpdateUserPreferenceDto dto, int userId);
        Task DeleteAsync(int id, int userId);

        // Idioma — atajo sobre el mismo almacenamiento genérico
        Task<LanguageResponseDto> GetLanguageAsync(int userId);
        Task<LanguageResponseDto> SetLanguageAsync(SetLanguageDto dto, int userId);
    }
}