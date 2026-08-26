// Modules/Orders/Services/OrderStatusHistoryService.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Mappings;

namespace SaborExpress.Modules.Orders.Services
{
    // Sin Create/Update manual: las filas las genera OrderService
    // automáticamente al llamar PATCH /Orders/{id}/status.
    public class OrderStatusHistoryService : IOrderStatusHistoryService
    {
        private readonly IOrderStatusHistoryRepository _historyRepository;

        public OrderStatusHistoryService(IOrderStatusHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
        }

        public async Task<List<OrderStatusHistoryResponseDto>> GetByOrderIdAsync(int orderId)
        {
            if (!await _historyRepository.OrderExistsAsync(orderId))
                throw new ArgumentException("El pedido no existe");

            var histories = await _historyRepository.GetByOrderIdAsync(orderId);
            return histories.Select(OrderStatusHistoryMapper.ToResponse).ToList();
        }
    }
}