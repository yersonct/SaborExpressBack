// Modules/Orders/Mappings/OrderDetailHistoryMapper.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Mappings
{
    public static class OrderDetailHistoryMapper
    {
        public static OrderDetailHistoryResponseDto ToResponse(OrderDetailHistory history)
        {
            return new OrderDetailHistoryResponseDto
            {
                Id = history.Id,
                OrderDetailId = history.OrderDetailId,
                Action = history.Action,
                OldValue = history.OldValue,
                NewValue = history.NewValue,
                Reason = history.Reason,
                ChangedByEmployeeId = history.ChangedByEmployeeId ?? 0,
                ChangedByEmployeeName = history.ChangedByEmployee == null
                    ? null
                    : $"{history.ChangedByEmployee.Name} {history.ChangedByEmployee.LastName}".Trim(),
                ChangedAt = history.ChangedAt
            };
        }
    }
}