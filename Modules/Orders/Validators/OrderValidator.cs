// Modules/Orders/Validators/OrderValidator.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Validators
{
    public class OrderValidator
    {
        private readonly IOrderRepository _orderRepository;

        // Transiciones válidas: la clave es el estado actual, el valor los estados a los que puede pasar
        private static readonly Dictionary<OrderStatus, OrderStatus[]> ValidTransitions = new()
        {
            [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new[] { OrderStatus.InPreparation, OrderStatus.Cancelled },
            [OrderStatus.InPreparation] = new[] { OrderStatus.Ready, OrderStatus.Cancelled },
            [OrderStatus.Ready] = new[] { OrderStatus.Delivered, OrderStatus.Cancelled },
            [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
        };

        public OrderValidator(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task ValidateCreateAsync(CreateOrderDto dto, int employeeId)
        {
            if (dto.BranchId <= 0)
                throw new ArgumentException("Debe indicar una sucursal válida");

            if (!await _orderRepository.BranchExistsAsync(dto.BranchId))
                throw new ArgumentException("La sucursal no existe");

            // NUEVO: antes no se validaba que el empleado que crea el pedido exista realmente.
            if (!await _orderRepository.EmployeeExistsAsync(employeeId))
                throw new ArgumentException("El empleado que intenta crear el pedido no existe.");

            if (dto.OrderType == OrderType.DineIn && !dto.TableId.HasValue)
                throw new ArgumentException("Un pedido en el local (DineIn) debe indicar una mesa");

            if (dto.TableId.HasValue && !await _orderRepository.TableExistsInBranchAsync(dto.TableId.Value, dto.BranchId))
                throw new ArgumentException("La mesa no existe en esta sucursal");

            if (dto.CustomerId.HasValue && !await _orderRepository.CustomerExistsAsync(dto.CustomerId.Value))
                throw new ArgumentException("El cliente no existe");
        }

        public async Task ValidateUpdateAsync(Order order, UpdateOrderDto dto)
        {
            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                throw new ArgumentException("No se puede editar un pedido entregado o cancelado");

            if (dto.OrderType == OrderType.DineIn && !dto.TableId.HasValue)
                throw new ArgumentException("Un pedido en el local (DineIn) debe indicar una mesa");

            if (dto.TableId.HasValue && !await _orderRepository.TableExistsInBranchAsync(dto.TableId.Value, order.BranchId))
                throw new ArgumentException("La mesa no existe en esta sucursal");
        }

        public void ValidateStatusChange(Order order, UpdateOrderStatusDto dto)
        {
            if (!ValidTransitions.TryGetValue(order.Status, out var allowed) || !allowed.Contains(dto.Status))
                throw new ArgumentException($"No se puede cambiar el estado de {order.Status} a {dto.Status}");
        }

        public void ValidateCancel(Order order, CancelOrderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Debe indicar el motivo de la cancelación");

            if (order.Status == OrderStatus.Delivered)
                throw new ArgumentException("No se puede cancelar un pedido ya entregado");

            if (order.Status == OrderStatus.Cancelled)
                throw new ArgumentException("Este pedido ya fue cancelado");
        }
    }
}