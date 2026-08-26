using SaborExpress.Modules.Permissions.DTOs;
using SaborExpress.Modules.Permissions.Interfaces;

namespace SaborExpress.Modules.Permissions.Validators
{
    public class PermissionValidator
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionValidator(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task ValidateCreateAsync(CreatePermissionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("El nombre del permiso es obligatorio.");

            if (int.TryParse(dto.Name, out _))
                throw new ArgumentException($"El nombre '{dto.Name}' no es válido para un permiso.");

            if (await _permissionRepository.ExistsByNameAsync(dto.Name))
                throw new ArgumentException($"El permiso '{dto.Name}' ya se encuentra registrado.");
        }

        public async Task ValidateUpdateAsync(int id, UpdatePermissionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("El nombre del permiso es obligatorio.");

            if (int.TryParse(dto.Name, out _))
                throw new ArgumentException($"El nombre '{dto.Name}' no es válido para un permiso.");

            if (await _permissionRepository.ExistsByNameAsync(dto.Name, id))
                throw new ArgumentException($"El permiso '{dto.Name}' ya se encuentra registrado.");
        }
    }
}