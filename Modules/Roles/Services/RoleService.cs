using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Roles.DTOs;
using SaborExpress.Modules.Roles.Interfaces;
using SaborExpress.Modules.Roles.Mappings;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.Roles.Validators;

namespace SaborExpress.Modules.Roles.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly RoleValidator _roleValidator;

        public RoleService(IRoleRepository roleRepository, RoleValidator roleValidator)
        {
            _roleRepository = roleRepository;
            _roleValidator = roleValidator;
        }

        public async Task<List<RoleResponseDto>> GetAllAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            return roles.Select(RoleMapper.ToResponse).ToList();
        }

        public async Task<RoleResponseDto> GetByIdAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
                throw new KeyNotFoundException("El rol no existe");

            return RoleMapper.ToResponse(role);
        }

        public async Task<RoleResponseDto> CreateAsync(CreateRoleDto dto)
        {
            await _roleValidator.ValidateCreateAsync(dto);

            var role = new Role
            {
                Name = dto.Name.ToUpper(),
                Description = dto.Description,
                RequiresCv = dto.RequiresCv,
                Status = true
            };

            await _roleRepository.AddAsync(role);
            return RoleMapper.ToResponse(role);
        }

        public async Task<RoleResponseDto> UpdateAsync(int id, UpdateRoleDto dto)
        {
            await _roleValidator.ValidateUpdateAsync(id, dto);

            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
                throw new KeyNotFoundException("El rol no existe");

            role.Name = dto.Name.ToUpper();
            role.Description = dto.Description;
            role.RequiresCv = dto.RequiresCv;
            role.Status = dto.Status;

            await _roleRepository.UpdateAsync(role);
            return RoleMapper.ToResponse(role);
        }

        public async Task DeleteAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
                throw new KeyNotFoundException("El rol no existe");

            var enUso = await _roleRepository.HasAssignedUsersAsync(id);
            if (enUso)
                throw new InvalidOperationException(
                    "No se puede eliminar este rol porque hay empleados asignados a él. " +
                    "Usa la opción de editar para desactivarlo (Status) en su lugar.");

            await _roleRepository.DeleteAsync(role);
        }
    }
}