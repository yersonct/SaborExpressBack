using SaborExpress.Modules.Roles.DTOs;
using SaborExpress.Modules.Roles.Interfaces;
using SaborExpress.Modules.Roles.Models;

namespace SaborExpress.Modules.Roles.Validators
{
    public class RoleValidator
    {
        private readonly IRoleRepository _roleRepository;

        public RoleValidator(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task ValidateCreateAsync(CreateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("El nombre del rol es obligatorio.");

            if (await _roleRepository.ExistsByNameAsync(dto.Name))
                throw new ArgumentException($"El rol '{dto.Name}' ya se encuentra registrado.");


            if (!Enum.TryParse<RoleName>(dto.Name.Trim(), ignoreCase: true, out _))
            {
                var valoresValidos = string.Join(", ", Enum.GetNames(typeof(RoleName)));
                throw new ArgumentException(
                    $"El rol '{dto.Name}' no es válido. Los valores permitidos son: {valoresValidos}.");
            }
        }

        public async Task ValidateUpdateAsync(int id, UpdateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("El nombre del rol es obligatorio.");

            if (await _roleRepository.ExistsByNameAsync(dto.Name, id))
                throw new ArgumentException($"El rol '{dto.Name}' ya se encuentra registrado.");

            if (!Enum.TryParse<RoleName>(dto.Name.Trim(), ignoreCase: true, out _))
            {
                var valoresValidos = string.Join(", ", Enum.GetNames(typeof(RoleName)));
                throw new ArgumentException(
                    $"El rol '{dto.Name}' no es válido. Los valores permitidos son: {valoresValidos}.");
            }

        }

           

    }
}