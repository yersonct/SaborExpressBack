// Modules/UserPreferences/Validators/UserPreferenceValidator.cs
using SaborExpress.Modules.UserPreferences.Constants;
using SaborExpress.Modules.UserPreferences.DTOs;
using SaborExpress.Modules.UserPreferences.Interfaces;

namespace SaborExpress.Modules.UserPreferences.Validators
{
    public class UserPreferenceValidator
    {
        private static readonly string[] AllowedDataTypes = { "string", "bool", "int", "decimal" };
        private readonly IUserPreferenceRepository _preferenceRepository;

        public UserPreferenceValidator(IUserPreferenceRepository preferenceRepository)
        {
            _preferenceRepository = preferenceRepository;
        }

        public async Task ValidateCreateAsync(CreateUserPreferenceDto dto, int userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Screen))
                throw new ArgumentException("La pantalla (Screen) es obligatoria");

            if (string.IsNullOrWhiteSpace(dto.Key))
                throw new ArgumentException("La clave (Key) es obligatoria");

            ValidateDataType(dto.DataType);

            // Si alguien intenta crear "Language" a mano por el endpoint genérico,
            // igual queda validado contra los 4 idiomas permitidos.
            if (dto.Screen == LanguageOptions.LanguageScreen && dto.Key == LanguageOptions.LanguageKey)
                ValidateLanguage(dto.Value);

            if (await _preferenceRepository.ExistsByUserScreenKeyAsync(userId, dto.Screen, dto.Key))
                throw new ArgumentException("Ya existe una preferencia con esa clave para esta pantalla");
        }

        public void ValidateUpdate(UpdateUserPreferenceDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Value))
                throw new ArgumentException("El valor es obligatorio");

            ValidateDataType(dto.DataType);
        }

        public void ValidateLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("El idioma es obligatorio");

            if (!LanguageOptions.Allowed.Contains(language.ToLower()))
                throw new ArgumentException(
                    $"Idioma inválido. Valores permitidos: {string.Join(", ", LanguageOptions.Allowed)}");
        }

        private void ValidateDataType(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
                throw new ArgumentException("El tipo de dato (DataType) es obligatorio");

            if (!AllowedDataTypes.Contains(dataType.ToLower()))
                throw new ArgumentException($"DataType inválido. Valores permitidos: {string.Join(", ", AllowedDataTypes)}");
        }
    }
}