// Modules/Orders/Services/OrderDetailService.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Mappings;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Orders.Validators;
using SaborExpress.Modules.Products.Interfaces; // ajusta si es distinto

namespace SaborExpress.Modules.Orders.Services
{
    public class OrderDetailService : IOrderDetailService
    {
        private readonly IOrderDetailRepository _orderDetailRepository;
        private readonly IOrderDetailHistoryRepository _historyRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly OrderDetailValidator _validator;

        // TODO: mover a Configurations (TAX_RATE) cuando ese módulo esté listo
        private const decimal TaxRate = 0.19m;

        public OrderDetailService(
            IOrderDetailRepository orderDetailRepository,
            IOrderDetailHistoryRepository historyRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            OrderDetailValidator validator)
        {
            _orderDetailRepository = orderDetailRepository;
            _historyRepository = historyRepository;
            _productRepository = productRepository;
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
            await _validator.ValidateCreateAsync(orderId, dto);

            var product = await _productRepository.GetByIdAsync(dto.ProductId);

            var orderDetail = new OrderDetail
            {
                OrderId = orderId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                Notes = dto.Notes,
                UnitPrice = product!.Price,
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
            var orderDetail = await _orderDetailRepository.GetByIdAsync(id);
            if (orderDetail == null)
                throw new ArgumentException("La línea del pedido no existe");

            await _validator.ValidateUpdate(orderDetail, dto); // ahora con await

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
            var orderDetail = await _orderDetailRepository.GetByIdAsync(id);
            if (orderDetail == null)
                throw new ArgumentException("La línea del pedido no existe");

            await _validator.ValidateVoid(orderDetail, dto); // ahora con await

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