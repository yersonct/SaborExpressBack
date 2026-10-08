using SaborExpress.Modules.UserPreferences.DTOs;
using SaborExpress.Modules.UserPreferences.Models;

namespace SaborExpress.Modules.UserPreferences.Mappings
{
    public static class UserSettingsMapper
    {
        public static UserSettingsResponseDto ToResponse(UserSettings s, bool isDefault = false) => new()
        {
            Theme = s.Theme,
            EmailNotifications = s.EmailNotifications,
            PushNotifications = s.PushNotifications,
            SoundNotifications = s.SoundNotifications,
            TimeZone = s.TimeZone,
            UpdatedAt = isDefault ? null : s.UpdatedAt,
            IsDefault = isDefault
        };
    }
}