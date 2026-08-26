using SaborExpress.Modules.Branches.DTOs;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.Branches.Mappings;
using SaborExpress.Modules.Branches.Models;
using SaborExpress.Modules.Branches.Validators;

namespace SaborExpress.Modules.Branches.Services
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly BranchValidator _validator;
        private readonly IBranchAccessGuard _accessGuard;

        public BranchService(
            IBranchRepository branchRepository,
            BranchValidator validator,
            IBranchAccessGuard accessGuard)
        {
            _branchRepository = branchRepository;
            _validator = validator;
            _accessGuard = accessGuard;
        }

        public async Task<BranchResponseDto> CreateAsync(CreateBranchDto dto)
        {
            await _validator.ValidateCreateAsync(dto);

            var branch = new Branch
            {
                Name = dto.Name.ToUpper(),
                Address = dto.Address,
                Phone = dto.Phone,
                Status = true
            };

            await _branchRepository.AddAsync(branch);

            // Una sede recién creada nunca tiene empleados todavía.
            return BranchMapper.ToResponse(branch, employeeCount: 0);
        }

        public async Task<BranchResponseDto> UpdateAsync(int id, UpdateBranchDto dto, int currentUserId)
        {
            await _validator.ValidateUpdateAsync(id, dto);

            var branch = await _branchRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("La sede no existe.");

            await _accessGuard.EnsureCanAccessBranchAsync(branch.Id, currentUserId);

            branch.Name = dto.Name.ToUpper();
            branch.Address = dto.Address;
            branch.Phone = dto.Phone;
            branch.Status = dto.Status;

            await _branchRepository.UpdateAsync(branch);

            var employeeCount = await _branchRepository.GetEmployeeCountAsync(branch.Id);
            return BranchMapper.ToResponse(branch, employeeCount);
        }

        public async Task DeleteAsync(int id)
        {
            var branch = await _branchRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("La sede no existe.");

            if (await _branchRepository.HasEmployeesAsync(id))
                throw new InvalidOperationException(
                    $"No se puede eliminar la sede '{branch.Name}' porque tiene empleados asignados. " +
                    "Usa la opción de editar para desactivarla (Status) en su lugar.");

            await _branchRepository.DeleteAsync(branch);
        }

        public async Task<BranchResponseDto> GetByIdAsync(int id)
        {
            var summary = await _branchRepository.GetSummaryByIdAsync(id)
                ?? throw new KeyNotFoundException("La sede no existe.");

            return BranchMapper.ToResponse(summary);
        }

        public async Task<List<BranchResponseDto>> GetAllAsync()
        {
            var summaries = await _branchRepository.GetAllSummariesAsync();
            return summaries.Select(BranchMapper.ToResponse).ToList();
        }

        public async Task<BranchResponseDto> GetMineAsync(int currentUserId)
        {
            var branchId = await _accessGuard.GetOwnBranchIdAsync(currentUserId)
                ?? throw new InvalidOperationException("El usuario actual no tiene una sede asignada.");

            var summary = await _branchRepository.GetSummaryByIdAsync(branchId)
                ?? throw new KeyNotFoundException("La sede no existe.");

            return BranchMapper.ToResponse(summary);
        }
    }
}