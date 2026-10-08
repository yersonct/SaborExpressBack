using Microsoft.EntityFrameworkCore;
using SaborExpress.Modules.UserPreferences.DTOs;
using SaborExpress.Modules.UserPreferences.Interfaces;
using SaborExpress.Modules.UserPreferences.Mappings;
using SaborExpress.Modules.UserPreferences.Models;
using SaborExpress.Modules.UserPreferences.Validators;

namespace SaborExpress.Modules.UserPreferences.Services
{
    public class UserSettingsService : IUserSettingsService
    {
        private readonly IUserSettingsRepository _repository;
        private readonly UserSettingsValidator _validator;

        public UserSettingsService(IUserSettingsRepository repository, UserSettingsValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<UserSettingsResponseDto> GetAsync(int userId)
        {
            var settings = await _repository.GetByUserIdAsync(userId);
            return settings is null
                ? UserSettingsMapper.ToResponse(new UserSettings { UserId = userId }, isDefault: true)
                : UserSettingsMapper.ToResponse(settings);
        }

        public async Task<UserSettingsResponseDto> UpdateAsync(UpdateUserSettingsDto dto, int userId)
        {
            _validator.Validate(dto);

            var settings = await _repository.GetByUserIdAsync(userId);
            var isNew = settings is null;
            settings ??= new UserSettings { UserId = userId };

            settings.Theme = dto.Theme.ToLower();
            settings.EmailNotifications = dto.EmailNotifications;
            settings.PushNotifications = dto.PushNotifications;
            settings.SoundNotifications = dto.SoundNotifications;
            settings.TimeZone = dto.TimeZone;
            settings.UpdatedAt = DateTime.UtcNow;

            if (isNew) await _repository.AddAsync(settings);

            try
            {
                await _repository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("No se pudo guardar la configuración. Intenta de nuevo.");
            }

            return UserSettingsMapper.ToResponse(settings);
        }
    }
}