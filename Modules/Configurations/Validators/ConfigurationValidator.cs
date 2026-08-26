// Modules/Configurations/Validators/ConfigurationValidator.cs
using SaborExpress.Modules.Configurations.DTOs;
using SaborExpress.Modules.Configurations.Interfaces;

namespace SaborExpress.Modules.Configurations.Validators
{
    public class ConfigurationValidator
    {
        private static readonly string[] AllowedDataTypes = { "string", "bool", "int", "decimal" };
        private readonly IConfigurationRepository _configurationRepository;

        public ConfigurationValidator(IConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;
        }

        public async Task ValidateCreateAsync(CreateConfigurationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Key))
                throw new ArgumentException("La clave (Key) es obligatoria");

            if (dto.BranchId.HasValue)
            {
                if (!await _configurationRepository.BranchExistsAsync(dto.BranchId.Value))
                    throw new ArgumentException("La sucursal no existe");
            }

            if (await _configurationRepository.ExistsByBranchAndKeyAsync(dto.BranchId, dto.Key))
                throw new ArgumentException("Ya existe una configuración con esa clave para esta sucursal (o global)");

            ValidateValueAndType(dto.Value, dto.DataType);
        }

        public void ValidateUpdate(UpdateConfigurationDto dto)
        {
            ValidateValueAndType(dto.Value, dto.DataType);
        }

        private void ValidateValueAndType(string value, string dataType)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("El valor es obligatorio");

            if (string.IsNullOrWhiteSpace(dataType))
                throw new ArgumentException("El tipo de dato (DataType) es obligatorio");

            if (!AllowedDataTypes.Contains(dataType.ToLower()))
                throw new ArgumentException($"DataType inválido. Valores permitidos: {string.Join(", ", AllowedDataTypes)}");
        }
    }
}