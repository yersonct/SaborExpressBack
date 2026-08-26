// Modules/UserPreferences/Services/UserPreferenceService.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Modules.UserPreferences.Constants;
using SaborExpress.Modules.UserPreferences.DTOs;
using SaborExpress.Modules.UserPreferences.Interfaces;
using SaborExpress.Modules.UserPreferences.Mappings;
using SaborExpress.Modules.UserPreferences.Models;
using SaborExpress.Modules.UserPreferences.Validators;

namespace SaborExpress.Modules.UserPreferences.Services
{
    public class UserPreferenceService : IUserPreferenceService
    {
        private readonly IUserPreferenceRepository _preferenceRepository;
        private readonly UserPreferenceValidator _validator;

        public UserPreferenceService(IUserPreferenceRepository preferenceRepository, UserPreferenceValidator validator)
        {
            _preferenceRepository = preferenceRepository;
            _validator = validator;
        }

        public async Task<List<UserPreferenceResponseDto>> GetByUserIdAsync(int userId)
        {
            var preferences = await _preferenceRepository.GetByUserIdAsync(userId);
            return preferences.Select(UserPreferenceMapper.ToResponse).ToList();
        }

        public async Task<UserPreferenceResponseDto> CreateAsync(CreateUserPreferenceDto dto, int userId)
        {
            await _validator.ValidateCreateAsync(dto, userId);

            var preference = new UserPreference
            {
                UserId = userId,
                Screen = dto.Screen,
                Key = dto.Key,
                Value = dto.Value,
                DataType = dto.DataType,
                UpdatedAt = DateTime.UtcNow
            };

            await _preferenceRepository.AddAsync(preference);

            try
            {
                await _preferenceRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Choca con el índice único (UserId+Screen+Key) por una condición de carrera
                throw new ArgumentException("Ya existe una preferencia con esa clave para esta pantalla");
            }

            return UserPreferenceMapper.ToResponse(preference);
        }

        public async Task<UserPreferenceResponseDto> UpdateAsync(int id, UpdateUserPreferenceDto dto, int userId)
        {
            var preference = await _preferenceRepository.GetByIdAsync(id);
            if (preference == null)
                throw new ArgumentException("La preferencia no existe");

            if (preference.UserId != userId)
                throw new ArgumentException("Esta preferencia no pertenece al usuario");

            _validator.ValidateUpdate(dto);

            preference.Value = dto.Value;
            preference.DataType = dto.DataType;
            preference.UpdatedAt = DateTime.UtcNow;

            await _preferenceRepository.UpdateAsync(preference);
            await _preferenceRepository.SaveChangesAsync();

            return UserPreferenceMapper.ToResponse(preference);
        }

        public async Task DeleteAsync(int id, int userId)
        {
            var preference = await _preferenceRepository.GetByIdAsync(id);
            if (preference == null)
                throw new ArgumentException("La preferencia no existe");

            if (preference.UserId != userId)
                throw new ArgumentException("Esta preferencia no pertenece al usuario");

            await _preferenceRepository.DeleteAsync(preference);
            await _preferenceRepository.SaveChangesAsync();
        }

        public async Task<LanguageResponseDto> GetLanguageAsync(int userId)
        {
            var preference = await _preferenceRepository.GetByUserScreenKeyAsync(
                userId, LanguageOptions.LanguageScreen, LanguageOptions.LanguageKey);

            return new LanguageResponseDto
            {
                Language = preference?.Value ?? LanguageOptions.DefaultLanguage
            };
        }

        public async Task<LanguageResponseDto> SetLanguageAsync(SetLanguageDto dto, int userId)
        {
            var language = dto.Language.ToLower();
            _validator.ValidateLanguage(language);

            var existing = await _preferenceRepository.GetByUserScreenKeyAsync(
                userId, LanguageOptions.LanguageScreen, LanguageOptions.LanguageKey);

            if (existing != null)
            {
                existing.Value = language;
                existing.UpdatedAt = DateTime.UtcNow;
                await _preferenceRepository.UpdateAsync(existing);
            }
            else
            {
                var preference = new UserPreference
                {
                    UserId = userId,
                    Screen = LanguageOptions.LanguageScreen,
                    Key = LanguageOptions.LanguageKey,
                    Value = language,
                    DataType = "string",
                    UpdatedAt = DateTime.UtcNow
                };
                await _preferenceRepository.AddAsync(preference);
            }

            await _preferenceRepository.SaveChangesAsync();

            return new LanguageResponseDto { Language = language };
        }
    }
}