using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Roles.Interfaces;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.UserRoles.DTOs;
using SaborExpress.Modules.UserRoles.Interfaces;
using SaborExpress.Modules.UserRoles.Mappings;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;

namespace SaborExpress.Modules.UserRoles.Services
{
    public class UserRoleService : IUserRoleService
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IRoleRepository _roleRepository;

        public UserRoleService(
            IUserRoleRepository userRoleRepository,
            IAuthRepository authRepository,
            IRoleRepository roleRepository)
        {
            _userRoleRepository = userRoleRepository;
            _authRepository = authRepository;
            _roleRepository = roleRepository;
        }

        public async Task<UserRoleResponseDto> AssignAsync(CreateUserRoleDto dto, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var esGerente = currentUser.HasRole(RoleNames.Gerente);
            var esAdministrador = currentUser.HasRole(RoleNames.Administrador);

            if (!esGerente && !esAdministrador)
                throw new InvalidOperationException("No tienes permiso para asignar roles.");

            var user = await _authRepository.GetByIdWithRelationsAsync(dto.UserId)
                ?? throw new KeyNotFoundException("El usuario no existe.");

            var role = await _roleRepository.GetByIdAsync(dto.RoleId)
                ?? throw new KeyNotFoundException("El rol no existe.");

            if (user.Customer != null)
                throw new InvalidOperationException("No se puede asignar un rol a un usuario que es cliente.");

            if (user.Employee == null)
                throw new InvalidOperationException("Solo se pueden asignar roles a usuarios que son empleados.");

            // Un Administrador (no Gerente) tiene restricciones adicionales
            if (!esGerente && esAdministrador)
            {
                if (role.Name == RoleNames.Administrador || role.Name == RoleNames.Gerente)
                    throw new InvalidOperationException(
                        "Un Administrador no puede asignar el rol de Administrador ni Gerente. Solo un Gerente puede hacerlo.");

                if (currentUser.Employee?.BranchId == null ||
                    user.Employee.BranchId != currentUser.Employee.BranchId)
                    throw new InvalidOperationException(
                        "Solo puedes asignar roles a empleados de tu misma sede.");
            }

            if (await _userRoleRepository.ExistsAsync(dto.UserId, dto.RoleId))
                throw new ArgumentException($"El usuario '{user.Email}' ya tiene asignado el rol '{role.Name}'.");

            if (role.Name == RoleNames.Administrador && user.Employee.BranchId.HasValue)
            {
                var yaTieneAdmin = await _userRoleRepository.ExistsAdministradorInBranchAsync(
                    user.Employee.BranchId.Value, excludeUserId: dto.UserId);

                if (yaTieneAdmin)
                    throw new InvalidOperationException(
                        "Esta sede ya tiene un Administrador asignado.");
            }

            var userRole = new UserRole
            {
                UserId = dto.UserId,
                RoleId = dto.RoleId,
                AssignedAt = DateTime.UtcNow
            };

            await _userRoleRepository.AddAsync(userRole);

            var created = await _userRoleRepository.GetAsync(dto.UserId, dto.RoleId)
                ?? throw new InvalidOperationException("Error al crear la asignaci�n de rol.");

            return UserRoleMapper.ToResponse(created);
        }

        public async Task RemoveAsync(int userId, int roleId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var esGerente = currentUser.HasRole(RoleNames.Gerente);
            var esAdministrador = currentUser.HasRole(RoleNames.Administrador);

            var userRole = await _userRoleRepository.GetAsync(userId, roleId)
                ?? throw new KeyNotFoundException("Esa relación usuario-rol no existe.");

            var role = await _roleRepository.GetByIdAsync(roleId)
                ?? throw new KeyNotFoundException("El rol no existe.");

            // Un Administrador (no Gerente) tiene las mismas restricciones que al asignar
            if (!esGerente && esAdministrador)
            {
                if (role.Name == RoleNames.Administrador || role.Name == RoleNames.Gerente)
                    throw new InvalidOperationException(
                        "Un Administrador no puede quitar el rol de Administrador ni Gerente. Solo un Gerente puede hacerlo.");

                var targetUser = await _authRepository.GetByIdWithRelationsAsync(userId)
                    ?? throw new KeyNotFoundException("El usuario no existe.");

                if (currentUser.Employee?.BranchId == null ||
                    targetUser.Employee?.BranchId != currentUser.Employee.BranchId)
                    throw new InvalidOperationException(
                        "Solo puedes quitar roles a empleados de tu misma sede.");
            }

            // No se puede dejar una sede sin ningún Administrador
            if (role.Name == RoleNames.Administrador)
            {
                var targetUser = await _authRepository.GetByIdWithRelationsAsync(userId)
                    ?? throw new KeyNotFoundException("El usuario no existe.");

                if (targetUser.Employee?.BranchId.HasValue == true)
                {
                    var hayOtroAdmin = await _userRoleRepository.ExistsAdministradorInBranchAsync(
                        targetUser.Employee.BranchId.Value, excludeUserId: userId);

                    if (!hayOtroAdmin)
                        throw new InvalidOperationException(
                            "No puedes quitar el rol de Administrador porque es el único de esta sede. " +
                            "Asigna otro Administrador antes de quitar este.");
                }
            }

            await _userRoleRepository.RemoveAsync(userRole);
        }

        public async Task<List<UserRoleResponseDto>> GetAllAsync()
        {
            var all = await _userRoleRepository.GetAllAsync();
            return all.Select(UserRoleMapper.ToResponse).ToList();
        }

        public async Task<List<UserRoleResponseDto>> GetByUserIdAsync(int userId)
        {
            var list = await _userRoleRepository.GetByUserIdAsync(userId);
            return list.Select(UserRoleMapper.ToResponse).ToList();
        }
    }
}