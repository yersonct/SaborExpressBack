using Microsoft.EntityFrameworkCore;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.Configurations.DTOs;
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Modules.Configurations.Mappings;
using SaborExpress.Modules.Configurations.Models;
using SaborExpress.Modules.Configurations.Validators;

namespace SaborExpress.Modules.Configurations.Services
{
    public class BranchOperationalSettingsService : IBranchOperationalSettingsService
    {
        private readonly IBranchOperationalSettingsRepository _repository;
        private readonly IBranchRepository _branchRepository;
        private readonly IBranchAccessGuard _accessGuard;
        private readonly BranchOperationalSettingsValidator _validator;

        public BranchOperationalSettingsService(
            IBranchOperationalSettingsRepository repository,
            IBranchRepository branchRepository,
            IBranchAccessGuard accessGuard,
            BranchOperationalSettingsValidator validator)
        {
            _repository = repository;
            _branchRepository = branchRepository;
            _accessGuard = accessGuard;
            _validator = validator;
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

            settings.OpeningTime = BranchOperationalSettingsValidator.ParseTime(dto.OpeningTime, "La hora de apertura");
            settings.ClosingTime = BranchOperationalSettingsValidator.ParseTime(dto.ClosingTime, "La hora de cierre");
            settings.TaxRate = dto.TaxRate;
            settings.DeliveryFee = dto.DeliveryFee;
            settings.SuggestedTipPercent = dto.SuggestedTipPercent;
            settings.DeliveryRadiusKm = dto.DeliveryRadiusKm;
            settings.MinOrderAmount = dto.MinOrderAmount;
            settings.AcceptsDelivery = dto.AcceptsDelivery;
            settings.AcceptsDineIn = dto.AcceptsDineIn;
            settings.UpdatedAt = DateTime.UtcNow;

            if (isNew) await _repository.AddAsync(settings);

            try
            {
                await _repository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("No se pudo guardar la configuración. Intenta de nuevo.");
            }

            return BranchOperationalSettingsMapper.ToResponse(settings);
        }
    }
}