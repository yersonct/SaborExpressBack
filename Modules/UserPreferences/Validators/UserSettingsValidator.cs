using SaborExpress.Modules.UserPreferences.DTOs;

namespace SaborExpress.Modules.UserPreferences.Validators
{
    public class UserSettingsValidator
    {
        private static readonly string[] AllowedThemes = { "light", "dark", "system" };

        public void Validate(UpdateUserSettingsDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Theme) || !AllowedThemes.Contains(dto.Theme.ToLower()))
                throw new ArgumentException($"Tema inválido. Valores permitidos: {string.Join(", ", AllowedThemes)}");

            if (string.IsNullOrWhiteSpace(dto.TimeZone))
                throw new ArgumentException("La zona horaria es obligatoria.");

            try { TimeZoneInfo.FindSystemTimeZoneById(dto.TimeZone); }
            catch (TimeZoneNotFoundException)
            {
                throw new ArgumentException("Zona horaria inválida (ej. America/Bogota).");
            }
            catch (InvalidTimeZoneException)
            {
                throw new ArgumentException("Zona horaria inválida (ej. America/Bogota).");
            }
        }
    }
}