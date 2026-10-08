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
        private readonly IGeocodingService _geocodingService;

        public BranchService(
            IBranchRepository branchRepository,
            BranchValidator validator,
            IBranchAccessGuard accessGuard,
            IGeocodingService geocodingService)
        {
            _branchRepository = branchRepository;
            _validator = validator;
            _accessGuard = accessGuard;
            _geocodingService = geocodingService;
        }

        public async Task<BranchResponseDto> CreateAsync(CreateBranchDto dto)
        {
            await _validator.ValidateCreateAsync(dto);

            // La dirección es obligatoria para poder ubicar la sede en el mapa
            if (string.IsNullOrWhiteSpace(dto.Address))
                throw new InvalidOperationException(
                    "La dirección es obligatoria: se usa para calcular la ubicación de la sede.");

            var (latitude, longitude) = await _geocodingService.GeocodeAsync(dto.Address);

            var branch = new Branch
            {
                Name = dto.Name.ToUpper(),
                Address = dto.Address,
                Phone = dto.Phone,
                Status = true,
                Latitude = latitude,
                Longitude = longitude
            };

            await _branchRepository.AddAsync(branch);

            // El código público depende del Id, así que se genera después de
            // insertar (AddAsync ya guarda y asigna el Id autoincremental).
            branch.PublicKitchenCode = GeneratePublicKitchenCode(branch);
            await _branchRepository.UpdateAsync(branch);

            // Una sede recién creada nunca tiene empleados todavía.
            return BranchMapper.ToResponse(branch, employeeCount: 0);
        }

        private static string GeneratePublicKitchenCode(Branch branch)
        {
            var slug = branch.Name
                .Replace("SEDE ", "")
                .Replace(" ", "-")
                .ToUpper();

            return $"{slug}-{branch.Id}";
        }
        public async Task<List<PublicBranchDto>> GetPublicActiveAsync()
        {
            var summaries = await _branchRepository.GetAllSummariesAsync();

            return summaries
                .Where(s => s.Status) // solo sedes activas
                .Select(s => new PublicBranchDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                })
                .ToList();
        }
        public async Task<BranchResponseDto> UpdateAsync(int id, UpdateBranchDto dto, int currentUserId)
        {
            await _validator.ValidateUpdateAsync(id, dto);

            var branch = await _branchRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("La sede no existe.");

            await _accessGuard.EnsureCanAccessBranchAsync(branch.Id, currentUserId);

            var addressChanged = !string.Equals(branch.Address, dto.Address, StringComparison.OrdinalIgnoreCase);

            branch.Name = dto.Name.ToUpper();
            branch.Address = dto.Address;
            branch.Phone = dto.Phone;

            // Solo el Gerente puede activar/desactivar una sede.
            // Si un Administrador manda otro valor, se rechaza.
            if (branch.Status != dto.Status)
            {
                if (!await _accessGuard.IsGerenteAsync(currentUserId))
                    throw new InvalidOperationException(
                        "Solo el Gerente puede activar o desactivar una sede.");

                branch.Status = dto.Status;
            }

            if (addressChanged)
            {
                if (string.IsNullOrWhiteSpace(dto.Address))
                    throw new InvalidOperationException(
                        "La dirección es obligatoria: se usa para calcular la ubicación de la sede.");

                var (latitude, longitude) = await _geocodingService.GeocodeAsync(dto.Address);
                branch.Latitude = latitude;
                branch.Longitude = longitude;
            }

            await _branchRepository.UpdateAsync(branch);

            var employeeCount = await _branchRepository.GetEmployeeCountAsync(branch.Id);
            return BranchMapper.ToResponse(branch, employeeCount);
        }

        public async Task DeleteAsync(int id)
        {
            var branch = await _branchRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("La sede no existe.");

            var motivos = new List<string>();

            if (await _branchRepository.HasEmployeesAsync(id))
                motivos.Add("tiene empleados asignados");

            if (await _branchRepository.HasTablesAsync(id))
                motivos.Add("tiene mesas registradas");

            if (await _branchRepository.HasOrdersAsync(id))
                motivos.Add("tiene pedidos registrados (activos o históricos)");

            if (await _branchRepository.HasSchedulesAsync(id))
                motivos.Add("tiene turnos registrados (activos o históricos)");

            if (motivos.Count > 0)
                throw new InvalidOperationException(
                    $"No se puede eliminar la sede '{branch.Name}' porque {string.Join(", ", motivos)}. " +
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
        public async Task<Branch> FindNearestBranchAsync(decimal latitude, decimal longitude)
        {
            var branches = await _branchRepository.GetAllActiveWithCoordinatesAsync();

            if (branches.Count == 0)
                throw new InvalidOperationException("No hay sedes activas disponibles.");

            Branch? nearest = null;
            double shortestDistance = double.MaxValue;

            foreach (var branch in branches)
            {
                var distance = CalculateHaversineDistanceKm(
                    (double)latitude, (double)longitude,
                    (double)branch.Latitude, (double)branch.Longitude);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearest = branch;
                }
            }

            return nearest!;
        }

        private static double CalculateHaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }
}