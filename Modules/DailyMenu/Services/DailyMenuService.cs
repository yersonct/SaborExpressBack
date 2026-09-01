// Modules/DailyMenu/Services/DailyMenuService.cs
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.DailyMenu.DTOs;
using SaborExpress.Modules.DailyMenu.Enum;
using SaborExpress.Modules.DailyMenu.Interfaces;
using SaborExpress.Modules.DailyMenu.Mappings;
using SaborExpress.Modules.DailyMenu.Models;
using SaborExpress.Modules.DailyMenu.Validators;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Modules.DailyMenu.Services
{
    public class DailyMenuService : IDailyMenuService
    {
        private readonly IDailyMenuRepository _repository;
        private readonly DailyMenuValidator _validator;
        private readonly IAuthRepository _authRepository;
        private readonly IAuthorizationService _authorizationService;

        public DailyMenuService(
            IDailyMenuRepository repository,
            DailyMenuValidator validator,
            IAuthRepository authRepository,
            IAuthorizationService authorizationService)
        {
            _repository = repository;
            _validator = validator;
            _authRepository = authRepository;
            _authorizationService = authorizationService;
        }

        public async Task<List<DailyMenuItemResponseDto>> GetByBranchAndDateAsync(int branchId, DateTime date, string? period, int currentUserId)
        {
            await EnsureCanViewBranchMenuAsync(branchId, currentUserId);

            var parsedPeriod = ParsePeriod(period);
            var items = await _repository.GetByBranchAndDateAsync(branchId, date, parsedPeriod);
            return items.Select(i => i.ToResponseDto()).ToList();
        }

        public async Task<List<DailyMenuItemResponseDto>> GetTodayAvailableAsync(int branchId, string? period)
        {
            var parsedPeriod = ParsePeriod(period);
            var items = await _repository.GetByBranchAndDateAsync(branchId, DateTime.UtcNow.Date, parsedPeriod);

            return items
                .Where(i => i.IsAvailable)
                .Select(i => i.ToResponseDto())
                .ToList();
        }

        public async Task<DailyMenuItemResponseDto> GetByIdAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El registro del menú del día no existe.");

            return item.ToResponseDto();
        }

        public async Task<DailyMenuItemResponseDto> CreateAsync(CreateDailyMenuItemDto dto, int currentUserId)
        {
            await EnsureCanManageMenuAsync(dto.BranchId, currentUserId);

            _validator.ValidateCreate(dto);

            if (await _repository.ExistsAsync(dto.BranchId, dto.ProductId, dto.Date, dto.MealPeriod))
                throw new ArgumentException("Este producto ya está registrado en el menú de esa fecha y franja.");

            var entity = new DailyMenuItem
            {
                BranchId = dto.BranchId,
                ProductId = dto.ProductId,
                Date = dto.Date.Date,
                MealPeriod = dto.MealPeriod,
                IsAvailable = dto.IsAvailable
            };

            await _repository.AddAsync(entity);

            var created = await _repository.GetByIdAsync(entity.Id)
                ?? throw new InvalidOperationException("Error al crear el registro del menú del día.");

            return created.ToResponseDto();
        }

        public async Task<List<DailyMenuItemResponseDto>> BulkSetAsync(BulkSetDailyMenuDto dto, int currentUserId)
        {
            await EnsureCanManageMenuAsync(dto.BranchId, currentUserId);

            _validator.ValidateBulk(dto);

            await _repository.RemoveRangeAsync(dto.BranchId, dto.Date, dto.MealPeriod);

            var newItems = dto.ProductIds.Select(productId => new DailyMenuItem
            {
                BranchId = dto.BranchId,
                ProductId = productId,
                Date = dto.Date.Date,
                MealPeriod = dto.MealPeriod,
                IsAvailable = true
            }).ToList();

            await _repository.AddRangeAsync(newItems);

            var result = await _repository.GetByBranchAndDateAsync(dto.BranchId, dto.Date, dto.MealPeriod);
            return result.Select(i => i.ToResponseDto()).ToList();
        }

        public async Task<DailyMenuItemResponseDto> UpdateAsync(int id, UpdateDailyMenuItemDto dto, int currentUserId)
        {
            var existing = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El registro del menú del día no existe.");

            await EnsureCanManageMenuAsync(existing.BranchId, currentUserId);

            _validator.ValidateUpdate(dto);

            existing.Date = dto.Date.Date;
            existing.MealPeriod = dto.MealPeriod;
            existing.IsAvailable = dto.IsAvailable;

            await _repository.UpdateAsync(existing);
            return existing.ToResponseDto();
        }

        public async Task<DailyMenuItemResponseDto> ToggleAvailabilityAsync(int id, int currentUserId)
        {
            var existing = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El registro del menú del día no existe.");

            await EnsureCanTogglePlateAsync(existing.BranchId, currentUserId);

            existing.IsAvailable = !existing.IsAvailable;

            await _repository.UpdateAsync(existing);
            return existing.ToResponseDto();
        }

        public async Task DeleteAsync(int id, int currentUserId)
        {
            var existing = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El registro del menú del día no existe.");

            await EnsureCanManageMenuAsync(existing.BranchId, currentUserId);

            await _repository.DeleteAsync(existing);
        }

        private static MealPeriod? ParsePeriod(string? period)
        {
            if (string.IsNullOrWhiteSpace(period))
                return null;

            if (System.Enum.TryParse<MealPeriod>(period, true, out var parsed))
                return parsed;

            throw new ArgumentException($"Franja inválida: '{period}'. Usa Desayuno, Almuerzo o Cena.");
        }

        // Para planear el menú (crear/editar/borrar/reemplazar todo el día):
        // solo Gerente (cualquier sede) o Administrador (su sede) con permiso GestionarMenu.
        private async Task EnsureCanManageMenuAsync(int branchId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (currentUser.HasRole(RoleNames.Gerente))
                return;

            if (currentUser.Employee?.BranchId != branchId)
                throw new InvalidOperationException("Solo puedes gestionar el menú de la sede a la que perteneces.");

            if (currentUser.HasRole(RoleNames.Administrador))
                return;

            throw new InvalidOperationException("No tienes permiso para gestionar el menú del día.");
        }

        // Para prender/apagar un plato a media jornada:
        // Gerente, Administrador (su sede), o Cocinero con permiso ActivarDesactivarPlato y turno activo.
        private async Task EnsureCanTogglePlateAsync(int branchId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (currentUser.HasRole(RoleNames.Gerente))
                return;

            if (currentUser.Employee?.BranchId != branchId)
                throw new InvalidOperationException("Solo puedes gestionar el menú de la sede a la que perteneces.");

            if (currentUser.HasRole(RoleNames.Administrador))
                return;

            var employeeId = currentUser.Employee?.Id
                ?? throw new InvalidOperationException("El usuario actual no tiene un perfil de empleado asociado.");

            var canToggle = await _authorizationService.CanPerformActionAsync(employeeId, PermissionNames.ActivarDesactivarPlato);
            if (!canToggle)
                throw new InvalidOperationException("No tienes permiso para activar/desactivar platos ahora mismo.");
        }

        private async Task EnsureCanViewBranchMenuAsync(int branchId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (currentUser.HasRole(RoleNames.Gerente))
                return;

            if (currentUser.Employee?.BranchId != branchId)
                throw new InvalidOperationException("Solo puedes ver el menú de la sede a la que perteneces.");
        }
    }
}