// Modules/Deliveries/Services/DeliveryService.cs
using SaborExpress.Modules.Deliveries.DTOs;
using SaborExpress.Modules.Deliveries.Enum;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Mappings;
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Deliveries.Validators;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Modules.Deliveries.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly IDeliveryRepository _deliveryRepository;
        private readonly DeliveryValidator _validator;
        private readonly IAuthorizationService _authorizationService;

        public DeliveryService(
            IDeliveryRepository deliveryRepository,
            DeliveryValidator validator,
            IAuthorizationService authorizationService)
        {
            _deliveryRepository = deliveryRepository;
            _validator = validator;
            _authorizationService = authorizationService;
        }

        public async Task<DeliveryResponseDto> CreateAsync(CreateDeliveryDto dto, int currentEmployeeId)
        {
            var canTake = await _authorizationService.CanPerformActionAsync(currentEmployeeId, PermissionNames.TomarEntrega);
            if (!canTake)
                throw new InvalidOperationException("No tienes permiso para tomar entregas ahora mismo.");

            await _validator.ValidateCreateAsync(dto, currentEmployeeId);

            var delivery = new Delivery
            {
                OrderId = dto.OrderId,
                AddressId = dto.AddressId,
                DeliveryPersonId = currentEmployeeId,
                Status = DeliveryStatus.Assigned,
                AssignedAt = DateTime.UtcNow
            };

            await _deliveryRepository.AddAsync(delivery);
            await _deliveryRepository.SaveChangesAsync();

            var created = await _deliveryRepository.GetByIdAsync(delivery.Id);
            return DeliveryMapper.ToResponse(created!);
        }

        public async Task<List<AvailableOrderDto>> GetAvailableOrdersAsync(int currentEmployeeId)
        {
            var branchId = await _deliveryRepository.GetEmployeeBranchIdAsync(currentEmployeeId)
                ?? throw new InvalidOperationException("No se pudo determinar tu sede.");

            var orders = await _deliveryRepository.GetAvailableOrdersAsync(branchId);
            return orders.Select(o => new AvailableOrderDto
            {
                OrderId = o.Id,
                BranchId = o.BranchId,
                Total = o.Total,
                CreatedAt = o.CreatedAt
            }).ToList();
        }

        public async Task<List<DeliveryResponseDto>> GetByOrderIdAsync(int orderId)
        {
            var deliveries = await _deliveryRepository.GetByOrderIdAsync(orderId);
            return deliveries.Select(DeliveryMapper.ToResponse).ToList();
        }

        public async Task<List<DeliveryResponseDto>> GetByDeliveryPersonIdAsync(int deliveryPersonId)
        {
            var deliveries = await _deliveryRepository.GetByDeliveryPersonIdAsync(deliveryPersonId);
            return deliveries.Select(DeliveryMapper.ToResponse).ToList();
        }

        public async Task<List<DeliveryResponseDto>> GetByBranchIdAsync(int branchId)
        {
            var deliveries = await _deliveryRepository.GetByBranchIdAsync(branchId);
            return deliveries.Select(DeliveryMapper.ToResponse).ToList();
        }

        public async Task<DeliveryResponseDto> UpdateStatusAsync(
            int id,
            UpdateDeliveryStatusDto dto,
            int currentEmployeeId,
            bool isAdmin)
        {
            var delivery = await _deliveryRepository.GetByIdAsync(id);
            if (delivery == null)
                throw new ArgumentException("El domicilio no existe");

            if (!isAdmin)
            {
                var canUpdate = await _authorizationService.CanPerformActionAsync(currentEmployeeId, PermissionNames.ActualizarEstadoEntrega);
                if (!canUpdate)
                    throw new InvalidOperationException("No tienes permiso para actualizar entregas ahora mismo.");
            }

            _validator.ValidateStatusChange(delivery, dto, currentEmployeeId, isAdmin);

            delivery.Status = dto.Status;

            if (dto.Status == DeliveryStatus.Delivered)
                delivery.DeliveredAt = DateTime.UtcNow;

            await _deliveryRepository.UpdateAsync(delivery);
            await _deliveryRepository.SaveChangesAsync();

            var updated = await _deliveryRepository.GetByIdAsync(delivery.Id);
            return DeliveryMapper.ToResponse(updated!);
        }
    }
}