using SaborExpress.Modules.Permissions.DTOs;
using SaborExpress.Modules.Permissions.Interfaces;
using SaborExpress.Modules.Permissions.Mappings;
using SaborExpress.Modules.Permissions.Models;
using SaborExpress.Modules.Permissions.Validators;
using Microsoft.EntityFrameworkCore;

namespace SaborExpress.Modules.Permissions.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly PermissionValidator _permissionValidator;

        public PermissionService(IPermissionRepository permissionRepository, PermissionValidator permissionValidator)
        {
            _permissionRepository = permissionRepository;
            _permissionValidator = permissionValidator;
        }

        public async Task<List<PermissionResponseDto>> GetAllAsync()
        {
            var permissions = await _permissionRepository.GetAllAsync();
            return permissions.Select(PermissionMapper.ToResponse).ToList();
        }

        public async Task<PermissionResponseDto> GetByIdAsync(int id)
        {
            var permission = await _permissionRepository.GetByIdAsync(id);
            if (permission == null)
                throw new KeyNotFoundException("El permiso no existe");

            return PermissionMapper.ToResponse(permission);
        }

        public async Task<PermissionResponseDto> CreateAsync(CreatePermissionDto dto)
        {
            await _permissionValidator.ValidateCreateAsync(dto);

            var permission = new Permission
            {
                Name = dto.Name.ToUpper(),
                Module = dto.Module,
                Description = dto.Description
            };

            await _permissionRepository.AddAsync(permission);
            return PermissionMapper.ToResponse(permission);
        }

        public async Task<PermissionResponseDto> UpdateAsync(int id, UpdatePermissionDto dto)
        {
            await _permissionValidator.ValidateUpdateAsync(id, dto);

            var permission = await _permissionRepository.GetByIdAsync(id);
            if (permission == null)
                throw new KeyNotFoundException("El permiso no existe");

            permission.Name = dto.Name.ToUpper();
            permission.Module = dto.Module;  
            permission.Description = dto.Description;
            permission.Status = dto.Status;
            await _permissionRepository.UpdateAsync(permission);
            return PermissionMapper.ToResponse(permission);
        }

// PermissionService.cs — reemplaza DeleteAsync
        public async Task DeleteAsync(int id)
        {
            var permission = await _permissionRepository.GetByIdAsync(id);
            if (permission == null)
                throw new KeyNotFoundException("El permiso no existe");

            var enUso = await _permissionRepository.HasAssignedRolesAsync(id);
            if (enUso)
                throw new InvalidOperationException(
                    $"No se puede eliminar el permiso '{permission.Name}' porque está asignado a uno o más roles.");

            try
            {
                await _permissionRepository.DeleteAsync(permission);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException(
                    $"No se puede eliminar el permiso '{permission.Name}' porque tiene registros asociados.");
            }
        }
    }
}