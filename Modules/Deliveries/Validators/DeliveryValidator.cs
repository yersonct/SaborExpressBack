// Modules/Deliveries/Validators/DeliveryValidator.cs
using SaborExpress.Modules.Deliveries.DTOs;
using SaborExpress.Modules.Deliveries.Enum;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;

namespace SaborExpress.Modules.Deliveries.Validators
{
    public class DeliveryValidator
    {
        private readonly IDeliveryRepository _deliveryRepository;
        private readonly IOrderRepository _orderRepository;

        private static readonly Dictionary<DeliveryStatus, DeliveryStatus[]> ValidTransitions = new()
        {
            [DeliveryStatus.Assigned] = new[] { DeliveryStatus.InTransit },
            [DeliveryStatus.InTransit] = new[] { DeliveryStatus.Delivered },
            [DeliveryStatus.Delivered] = Array.Empty<DeliveryStatus>()
        };

        public DeliveryValidator(
            IDeliveryRepository deliveryRepository,
            IOrderRepository orderRepository)
        {
            _deliveryRepository = deliveryRepository;
            _orderRepository = orderRepository;
        }

        // CAMBIADO: ahora recibe currentEmployeeId (quien toma el pedido), no viene del dto
       public async Task ValidateCreateAsync(CreateDeliveryDto dto, int currentEmployeeId)
        {
            if (dto.OrderId <= 0)
                throw new ArgumentException("Debe indicar un pedido válido");

            if (dto.AddressId <= 0)
                throw new ArgumentException("Debe indicar una dirección válida");

            var order = await _orderRepository.GetByIdAsync(dto.OrderId);
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            if (order.OrderType != OrderType.Delivery)
                throw new ArgumentException("Solo se puede tomar repartos de pedidos tipo Delivery");

            if (order.Status != OrderStatus.Ready && order.Status != OrderStatus.Confirmed)
                throw new ArgumentException(
                    $"No se puede tomar un pedido en estado {order.Status}. " +
                    "El pedido debe estar Confirmado o Listo.");

            if (await _deliveryRepository.OrderHasDeliveryAsync(dto.OrderId))
                throw new ArgumentException("Este pedido ya fue tomado por otro repartidor");

            if (!await _deliveryRepository.AddressExistsAsync(dto.AddressId))
                throw new ArgumentException("La dirección no existe");

            if (order.CustomerId.HasValue &&
                !await _deliveryRepository.AddressBelongsToCustomerAsync(dto.AddressId, order.CustomerId.Value))
            {
                throw new ArgumentException("La dirección no pertenece al cliente de este pedido");
            }

            // Ya no valida rol aquí — lo hace CanPerformActionAsync en el servicio, con turno incluido
        }

        // CAMBIADO: isAdmin en vez de isManagerOrAdmin — el Gerente ya NO tiene esta excepción
        public void ValidateStatusChange(
            Delivery delivery,
            UpdateDeliveryStatusDto dto,
            int currentEmployeeId,
            bool isAdmin)
        {
            if (!isAdmin && delivery.DeliveryPersonId != currentEmployeeId)
                throw new UnauthorizedAccessException("Solo el repartidor asignado puede actualizar este domicilio");

            if (!ValidTransitions.TryGetValue(delivery.Status, out var allowed) || !allowed.Contains(dto.Status))
                throw new ArgumentException($"No se puede cambiar el estado de {delivery.Status} a {dto.Status}");
        }
    }
}