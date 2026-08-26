// Modules/UserPreferences/Mappings/UserPreferenceMapper.cs
using SaborExpress.Modules.UserPreferences.DTOs;
using SaborExpress.Modules.UserPreferences.Models;

namespace SaborExpress.Modules.UserPreferences.Mappings
{
    public static class UserPreferenceMapper
    {
        public static UserPreferenceResponseDto ToResponse(UserPreference preference)
        {
            return new UserPreferenceResponseDto
            {
                Id = preference.Id,
                UserId = preference.UserId,
                Screen = preference.Screen,
                Key = preference.Key,
                Value = preference.Value,
                DataType = preference.DataType,
                UpdatedAt = preference.UpdatedAt
            };
        }
    }
}