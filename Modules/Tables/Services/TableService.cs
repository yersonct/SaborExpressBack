// Modules/Tables/Services/TableService.cs
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Tables.DTOs;
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Modules.Tables.Mappings;
using SaborExpress.Modules.Tables.Models;
using SaborExpress.Modules.Tables.Validators;
using SaborExpress.Modules.Tables.Enum;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Modules.Tables.Services
{
    public class TableService : ITableService
    {
        private readonly ITableRepository _tableRepository;
        private readonly TableValidator _validator;
        private readonly IAuthRepository _authRepository;
        private readonly IAuthorizationService _authorizationService;   

        public TableService(
            ITableRepository tableRepository,
            TableValidator validator,
            IAuthRepository authRepository,
            IAuthorizationService authorizationService)                
        {
            _tableRepository = tableRepository;
            _validator = validator;
            _authRepository = authRepository;
            _authorizationService = authorizationService;              
        }

        public async Task<List<TableResponseDto>> GetByBranchIdAsync(int branchId)
        {
            var tables = await _tableRepository.GetByBranchIdAsync(branchId);
            return tables.Select(TableMapper.ToResponse).ToList();
        }

        public async Task<TableResponseDto> GetByIdAsync(int id)
        {
            var table = await _tableRepository.GetByIdAsync(id);
            if (table == null)
                throw new ArgumentException("La mesa no existe");

            return TableMapper.ToResponse(table);
        }

        public async Task<TableResponseDto> CreateAsync(CreateTableDto dto, int currentUserId)
        {
            await _validator.ValidateCreateAsync(dto);
            await EnsureCanManageTableAsync(dto.BranchId, currentUserId);

            var table = new Table
            {
                BranchId = dto.BranchId,
                Number = dto.Number
            };

            await _tableRepository.AddAsync(table);
            await _tableRepository.SaveChangesAsync();

            var created = await _tableRepository.GetByIdAsync(table.Id);
            return TableMapper.ToResponse(created!);
        }

        public async Task<TableResponseDto> UpdateAsync(int id, UpdateTableDto dto, int currentUserId)
        {
            var table = await _tableRepository.GetByIdAsync(id);
            if (table == null)
                throw new ArgumentException("La mesa no existe");

            await EnsureCanManageTableAsync(table.BranchId, currentUserId);
            await _validator.ValidateUpdateAsync(table, dto);

            table.Number = dto.Number;

            await _tableRepository.UpdateAsync(table);
            await _tableRepository.SaveChangesAsync();

            return TableMapper.ToResponse(table);
        }

        public async Task<TableResponseDto> UpdateStatusAsync(int id, UpdateTableStatusDto dto, int currentUserId)
        {
            var table = await _tableRepository.GetByIdAsync(id);
            if (table == null)
                throw new ArgumentException("La mesa no existe");

            await EnsureCanUpdateTableStatusAsync(table.BranchId, currentUserId);   
            _validator.ValidateStatusChange(table, dto);

            table.Status = dto.Status;

            await _tableRepository.UpdateAsync(table);
            await _tableRepository.SaveChangesAsync();

            return TableMapper.ToResponse(table);
        }

        public async Task DeleteAsync(int id, int currentUserId)
        {
            var table = await _tableRepository.GetByIdAsync(id);
            if (table == null)
                throw new ArgumentException("La mesa no existe");

            await EnsureCanManageTableAsync(table.BranchId, currentUserId);
            _validator.ValidateDelete(table);

            await _tableRepository.DeleteAsync(table);
            await _tableRepository.SaveChangesAsync();
        }

        // Para Create/Update/Delete: estructura de la mesa en sí.
        // Solo Gerente (cualquier sede) o Administrador (su propia sede).
        private async Task EnsureCanManageTableAsync(int branchId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (currentUser.HasRole(RoleNames.Gerente))
                return;

            if (currentUser.Employee?.BranchId != branchId)
                throw new InvalidOperationException("Solo puedes gestionar mesas de la sede a la que perteneces.");
        }

        // Para UpdateStatus: operación del día a día.
        // Gerente (cualquier sede), Administrador (su sede), o Mesero con el
        // permiso CambiarEstadoMesa y turno activo, siempre dentro de su propia sede.
        private async Task EnsureCanUpdateTableStatusAsync(int branchId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (currentUser.HasRole(RoleNames.Gerente))
                return;

            if (currentUser.Employee?.BranchId != branchId)
                throw new InvalidOperationException("Solo puedes gestionar mesas de la sede a la que perteneces.");

            if (currentUser.HasRole(RoleNames.Administrador))
                return;

            var employeeId = currentUser.Employee?.Id
                ?? throw new InvalidOperationException("El usuario actual no tiene un perfil de empleado asociado.");

            var canUpdateStatus = await _authorizationService.CanPerformActionAsync(employeeId, PermissionNames.CambiarEstadoMesa);
            if (!canUpdateStatus)
                throw new InvalidOperationException("No tienes permiso para cambiar el estado de las mesas ahora mismo.");
        }

    }
}