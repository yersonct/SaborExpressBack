using Microsoft.EntityFrameworkCore;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.Configurations.DTOs;
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Modules.Configurations.Mappings;
using SaborExpress.Modules.Configurations.Models;
using SaborExpress.Modules.Configurations.Validators;
using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Shared.Helpers;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Modules.Deliveries.Interfaces;

namespace SaborExpress.Modules.Configurations.Services
{
    public class BranchOperationalSettingsService : IBranchOperationalSettingsService
    {
        private readonly IBranchOperationalSettingsRepository _repository;
        private readonly IBranchRepository _branchRepository;
        private readonly IBranchAccessGuard _accessGuard;
        private readonly BranchOperationalSettingsValidator _validator;
        private readonly IEmployeeScheduleRepository _scheduleRepository;
        private readonly IRealtimeNotifier _realtime;
        private readonly IDeliveryAssignmentService _deliveryAssignmentService;
        private readonly ILogger<BranchOperationalSettingsService> _logger;

        public BranchOperationalSettingsService(
            IBranchOperationalSettingsRepository repository,
            IBranchRepository branchRepository,
            IBranchAccessGuard accessGuard,
            BranchOperationalSettingsValidator validator,
            IEmployeeScheduleRepository scheduleRepository,
            IRealtimeNotifier realtime,
            IDeliveryAssignmentService deliveryAssignmentService,
            ILogger<BranchOperationalSettingsService> logger)
        {
            _repository = repository;
            _branchRepository = branchRepository;
            _accessGuard = accessGuard;
            _validator = validator;
            _scheduleRepository = scheduleRepository;
            _realtime = realtime;
            _deliveryAssignmentService = deliveryAssignmentService;
            _logger = logger;
        }

        public async Task<BranchOperationalSettingsResponseDto> GetAsync(int branchId)
        {
            _ = await _branchRepository.GetByIdAsync(branchId)
                ?? throw new KeyNotFoundException("La sede no existe.");

            var settings = await _repository.GetByBranchIdAsync(branchId);
            return settings is null
                ? BranchOperationalSettingsMapper.Default(branchId)
                : BranchOperationalSettingsMapper.ToResponse(settings);
        }

        public async Task<BranchOperationalSettingsResponseDto> UpdateAsync(
            int branchId, UpdateBranchOperationalSettingsDto dto, int currentUserId)
        {
            _ = await _branchRepository.GetByIdAsync(branchId)
                ?? throw new KeyNotFoundException("La sede no existe.");

            await _accessGuard.EnsureCanAccessBranchAsync(branchId, currentUserId);
            _validator.Validate(dto);

            var settings = await _repository.GetByBranchIdAsync(branchId);
            var isNew = settings is null;
            settings ??= new BranchOperationalSettings { BranchId = branchId };

            var openingTime = BranchOperationalSettingsValidator.ParseTime(dto.OpeningTime, "La hora de apertura");
            var closingTime = BranchOperationalSettingsValidator.ParseTime(dto.ClosingTime, "La hora de cierre");
            settings.OpeningTime = openingTime;
            settings.ClosingTime = closingTime;
            settings.TaxRate = dto.TaxRate;
            settings.DeliveryFee = dto.DeliveryFee;
            settings.DeliveryRadiusKm = dto.DeliveryRadiusKm;
            if (dto.MaxActiveDeliveriesPerCourier.HasValue)
                settings.MaxActiveDeliveriesPerCourier = dto.MaxActiveDeliveriesPerCourier.Value;
            settings.UpdatedAt = DateTime.UtcNow;

            if (isNew) await _repository.AddAsync(settings);

            try
            {
                await _repository.SaveChangesAsync();
                // Los turnos de hoy y futuros usan siempre el horario vigente.
                // Los días pasados conservan el horario que realmente tuvieron.
                var todayInColombia = DateOnly.FromDateTime(ColombiaTime.Now);
                await _scheduleRepository.ApplyBranchHoursToUpcomingAsync(
                    branchId,
                    todayInColombia,
                    openingTime,
                    closingTime);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("No se pudo guardar la configuración. Intenta de nuevo.");
            }

            await _realtime.AdminDataChangedAsync("EmployeeSchedules");
            await _realtime.AdminDataChangedAsync("Employees");
            await _realtime.BranchOperationalSettingsChangedAsync(branchId);
            await _realtime.PublicBranchesChangedAsync();
            try
            {
                await _deliveryAssignmentService.AssignPendingForBranchAsync(branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudieron asignar pedidos pendientes después de actualizar la capacidad de la sede {BranchId}.",
                    branchId);
            }
            return BranchOperationalSettingsMapper.ToResponse(settings);
        }
    }
}
