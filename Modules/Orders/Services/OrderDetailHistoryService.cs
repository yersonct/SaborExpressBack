// Modules/Orders/Services/OrderDetailHistoryService.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Mappings;

namespace SaborExpress.Modules.Orders.Services
{
    // No hay Create/Update manual: las filas las genera OrderDetailService
    // automáticamente al crear, actualizar o anular una línea.
    public class OrderDetailHistoryService : IOrderDetailHistoryService
    {
        private readonly IOrderDetailHistoryRepository _historyRepository;

        public OrderDetailHistoryService(IOrderDetailHistoryRepository historyRepository)
        {
            _historyRepository = historyRepository;
        }

        public async Task<List<OrderDetailHistoryResponseDto>> GetByOrderDetailIdAsync(int orderDetailId)
        {
            if (!await _historyRepository.OrderDetailExistsAsync(orderDetailId))
                throw new ArgumentException("La línea del pedido no existe");

            var histories = await _historyRepository.GetByOrderDetailIdAsync(orderDetailId);
            return histories.Select(OrderDetailHistoryMapper.ToResponse).ToList();
        }
    }
}