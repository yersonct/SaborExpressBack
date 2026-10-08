// Modules/Configurations/Services/ConfigurationService.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Modules.Configurations.DTOs;
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Modules.Configurations.Mappings;
using SaborExpress.Modules.Configurations.Models;
using SaborExpress.Modules.Configurations.Validators;

namespace SaborExpress.Modules.Configurations.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly ConfigurationValidator _validator;

        public ConfigurationService(IConfigurationRepository configurationRepository, ConfigurationValidator validator)
        {
            _configurationRepository = configurationRepository;
            _validator = validator;
        }

        public async Task<List<ConfigurationResponseDto>> GetAllAsync()
        {
            var configurations = await _configurationRepository.GetAllAsync();
            return configurations.Select(ConfigurationMapper.ToResponse).ToList();
        }

        public async Task<List<ConfigurationResponseDto>> GetByBranchIdAsync(int branchId)
        {
            var configurations = await _configurationRepository.GetByBranchIdAsync(branchId);
            return configurations.Select(ConfigurationMapper.ToResponse).ToList();
        }

        public async Task<ConfigurationResponseDto> GetByKeyAsync(int? branchId, string key)
        {
            var configuration = await _configurationRepository.GetByKeyAsync(branchId, key);
            if (configuration == null)
                throw new ArgumentException("No existe una configuración con esa clave");

            return ConfigurationMapper.ToResponse(configuration);
        }

        public async Task<ConfigurationResponseDto> CreateAsync(CreateConfigurationDto dto)
        {
            await _validator.ValidateCreateAsync(dto);

            var configuration = new BranchSetting
            {
                BranchId = dto.BranchId,
                Key = dto.Key,
                Value = dto.Value,
                DataType = dto.DataType,
                Description = dto.Description,
                UpdatedAt = DateTime.UtcNow
            };

            await _configurationRepository.AddAsync(configuration);

            try
            {
                await _configurationRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ArgumentException("Ya existe una configuración con esa clave para esta sucursal (o global)");
            }

            var created = await _configurationRepository.GetByIdAsync(configuration.Id);
            return ConfigurationMapper.ToResponse(created!);
        }

        public async Task<ConfigurationResponseDto> UpdateAsync(int id, UpdateConfigurationDto dto)
        {
            var configuration = await _configurationRepository.GetByIdAsync(id);
            if (configuration == null)
                throw new ArgumentException("La configuración no existe");

            _validator.ValidateUpdate(dto);

            configuration.Value = dto.Value;
            configuration.DataType = dto.DataType;
            configuration.Description = dto.Description;
            configuration.UpdatedAt = DateTime.UtcNow;

            await _configurationRepository.UpdateAsync(configuration);
            await _configurationRepository.SaveChangesAsync();

            return ConfigurationMapper.ToResponse(configuration);
        }
                public Task<int?> GetEmployeeBranchIdAsync(int employeeId)
        {
            return _configurationRepository.GetEmployeeBranchIdAsync(employeeId);
        }

        public Task<int?> GetBranchIdByConfigurationIdAsync(int configurationId)
        {
            return _configurationRepository.GetBranchIdByConfigurationIdAsync(configurationId);
        }
        public async Task DeleteAsync(int id)
        {
            var configuration = await _configurationRepository.GetByIdAsync(id);
            if (configuration == null)
                throw new ArgumentException("La configuración no existe");

            await _configurationRepository.DeleteAsync(configuration);
            await _configurationRepository.SaveChangesAsync();
        }
    }
}