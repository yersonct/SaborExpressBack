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
using Npgsql;
using SaborExpress.Shared.Exceptions;

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
            ApplySettings(settings);

            if (isNew) await _repository.AddAsync(settings);

            try
            {
                await _repository.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (isNew && IsBranchSettingsUniqueConflict(ex))
            {
                // Dos guardados simultáneos pueden intentar crear la primera fila.
                // El índice único protege la sede; al perder la carrera, se vuelve
                // a leer la fila creada por la otra solicitud y se actualiza.
                _repository.Detach(settings);
                var createdByConcurrentRequest = await _repository.GetByBranchIdAsync(branchId);
                if (createdByConcurrentRequest is null)
                {
                    _logger.LogError(ex,
                        "Falló la creación concurrente de configuración operativa para la sede {BranchId}.",
                        branchId);
                    throw new PersistenceException("No se pudo guardar la configuración. Intenta de nuevo.", ex);
                }

                settings = createdByConcurrentRequest;
                ApplySettings(settings);
                try
                {
                    await _repository.SaveChangesAsync();
                }
                catch (DbUpdateException retryException)
                {
                    _logger.LogError(retryException,
                        "Falló el reintento de configuración operativa para la sede {BranchId}.",
                        branchId);
                    throw new PersistenceException("No se pudo guardar la configuración. Intenta de nuevo.", retryException);
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "La base de datos rechazó la configuración operativa de la sede {BranchId}.",
                    branchId);
                throw new PersistenceException("No se pudo guardar la configuración. Intenta de nuevo.", ex);
            }

            // Los turnos de hoy y futuros usan siempre el horario vigente;
            // los días pasados conservan el horario que realmente tuvieron.
            try
            {
                var todayInColombia = DateOnly.FromDateTime(ColombiaTime.Now);
                await _scheduleRepository.ApplyBranchHoursToUpcomingAsync(
                    branchId,
                    todayInColombia,
                    openingTime,
                    closingTime);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "Se guardó la configuración de la sede {BranchId}, pero falló la actualización del horario de sus turnos.",
                    branchId);
                throw new PersistenceException("La configuración se guardó, pero no se pudieron actualizar los turnos. Intenta de nuevo.", ex);
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

            void ApplySettings(BranchOperationalSettings target)
            {
                target.OpeningTime = openingTime;
                target.ClosingTime = closingTime;
                target.TaxRate = dto.TaxRate;
                target.DeliveryFee = dto.DeliveryFee;
                target.DeliveryRadiusKm = dto.DeliveryRadiusKm;
                if (dto.MaxActiveDeliveriesPerCourier.HasValue)
                    target.MaxActiveDeliveriesPerCourier = dto.MaxActiveDeliveriesPerCourier.Value;
                target.UpdatedAt = DateTime.UtcNow;
            }
        }

        private static bool IsBranchSettingsUniqueConflict(DbUpdateException exception)
        {
            for (var cause = exception.InnerException; cause is not null; cause = cause.InnerException)
            {
                if (cause is PostgresException postgresException &&
                    postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                    postgresException.ConstraintName == "IX_branch_operational_settings_branch_id")
                    return true;
            }
            return false;
        }
    }
}
