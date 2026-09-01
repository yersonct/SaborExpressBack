// Modules/Orders/Services/OrderDetailService.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Mappings;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Orders.Validators;

namespace SaborExpress.Modules.Orders.Services
{
    public class OrderDetailService : IOrderDetailService
    {
        private readonly IOrderDetailRepository _orderDetailRepository;
        private readonly IOrderDetailHistoryRepository _historyRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly OrderDetailValidator _validator;

        // TODO: mover a Configurations (TAX_RATE) cuando se defina el rate por sede
        private const decimal TaxRate = 0.19m;

        public OrderDetailService(
            IOrderDetailRepository orderDetailRepository,
            IOrderDetailHistoryRepository historyRepository,
            IOrderRepository orderRepository,
            OrderDetailValidator validator)
        {
            _orderDetailRepository = orderDetailRepository;
            _historyRepository = historyRepository;
            _orderRepository = orderRepository;
            _validator = validator;
        }

        public async Task<List<OrderDetailResponseDto>> GetByOrderIdAsync(int orderId)
        {
            var details = await _orderDetailRepository.GetByOrderIdAsync(orderId);
            return details.Select(OrderDetailMapper.ToResponse).ToList();
        }

        public async Task<OrderDetailResponseDto> CreateAsync(int orderId, CreateOrderDetailDto dto, int employeeId)
        {
            // El validator ya trajo y valido el producto; no hace falta pedirlo de nuevo
            var product = await _validator.ValidateCreateAsync(orderId, dto);

            var orderDetail = new OrderDetail
            {
                OrderId = orderId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                Notes = dto.Notes,
                UnitPrice = product.Price,
                SubTotal = product.Price * dto.Quantity,
                LastModifiedByEmployeeId = employeeId,
                Status = OrderDetailStatus.Pending
            };

            await _orderDetailRepository.AddAsync(orderDetail);
            await _orderDetailRepository.SaveChangesAsync();

            await _historyRepository.AddAsync(new OrderDetailHistory
            {
                OrderDetailId = orderDetail.Id,
                ChangedByEmployeeId = employeeId,
                Action = OrderDetailHistoryAction.Created,
                NewValue = $"Qty: {orderDetail.Quantity}, Notes: {orderDetail.Notes}",
                ChangedAt = DateTime.UtcNow
            });
            await _historyRepository.SaveChangesAsync();

            await _orderRepository.RecalculateTotalsAsync(orderId, TaxRate);

            var created = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
            return OrderDetailMapper.ToResponse(created!);
        }

        public async Task<OrderDetailResponseDto> UpdateAsync(int id, UpdateOrderDetailDto dto, int employeeId)
        {
            var orderDetail = await _orderDetailRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("La linea del pedido no existe");

            await _validator.ValidateUpdate(orderDetail, dto);

            var oldValue = $"Qty: {orderDetail.Quantity}, Notes: {orderDetail.Notes}";

            orderDetail.Quantity = dto.Quantity;
            orderDetail.Notes = dto.Notes;
            orderDetail.SubTotal = orderDetail.UnitPrice * dto.Quantity;
            orderDetail.LastModifiedByEmployeeId = employeeId;

            await _orderDetailRepository.UpdateAsync(orderDetail);
            await _orderDetailRepository.SaveChangesAsync();

            await _historyRepository.AddAsync(new OrderDetailHistory
            {
                OrderDetailId = orderDetail.Id,
                ChangedByEmployeeId = employeeId,
                Action = OrderDetailHistoryAction.Updated,
                OldValue = oldValue,
                NewValue = $"Qty: {orderDetail.Quantity}, Notes: {orderDetail.Notes}",
                ChangedAt = DateTime.UtcNow
            });
            await _historyRepository.SaveChangesAsync();

            await _orderRepository.RecalculateTotalsAsync(orderDetail.OrderId, TaxRate);

            var updated = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
            return OrderDetailMapper.ToResponse(updated!);
        }

        public async Task<OrderDetailResponseDto> VoidAsync(int id, VoidOrderDetailDto dto, int employeeId)
        {
            var orderDetail = await _orderDetailRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("La linea del pedido no existe");

            await _validator.ValidateVoid(orderDetail, dto);

            var oldStatus = orderDetail.Status;
            orderDetail.Status = OrderDetailStatus.Voided;
            orderDetail.LastModifiedByEmployeeId = employeeId;

            await _orderDetailRepository.UpdateAsync(orderDetail);
            await _orderDetailRepository.SaveChangesAsync();

            await _historyRepository.AddAsync(new OrderDetailHistory
            {
                OrderDetailId = orderDetail.Id,
                ChangedByEmployeeId = employeeId,
                Action = OrderDetailHistoryAction.Voided,
                OldValue = oldStatus.ToString(),
                NewValue = OrderDetailStatus.Voided.ToString(),
                Reason = dto.Reason,
                ChangedAt = DateTime.UtcNow
            });
            await _historyRepository.SaveChangesAsync();

            await _orderRepository.RecalculateTotalsAsync(orderDetail.OrderId, TaxRate);

            var voided = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
            return OrderDetailMapper.ToResponse(voided!);
        }
    }
}