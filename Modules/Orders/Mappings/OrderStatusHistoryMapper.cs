// Modules/Orders/Mappings/OrderStatusHistoryMapper.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Mappings
{
    public static class OrderStatusHistoryMapper
    {
        public static OrderStatusHistoryResponseDto ToResponse(OrderStatusHistory history)
        {
            return new OrderStatusHistoryResponseDto
            {
                Id = history.Id,
                OrderId = history.OrderId,
                Status = history.Status,
                Notes = history.Notes,
                ChangedByEmployeeId = history.ChangedByEmployeeId,
                ChangedByEmployeeName = history.ChangedByEmployee == null
                    ? null
                    : $"{history.ChangedByEmployee.Name} {history.ChangedByEmployee.LastName}".Trim(),
                ChangedAt = history.ChangedAt
            };
        }
    }
}