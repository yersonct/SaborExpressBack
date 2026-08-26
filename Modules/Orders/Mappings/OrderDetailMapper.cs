// Modules/Orders/Mappings/OrderDetailMapper.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Mappings
{
    public static class OrderDetailMapper
    {
        public static OrderDetailResponseDto ToResponse(OrderDetail orderDetail)
        {
            return new OrderDetailResponseDto
            {
                Id = orderDetail.Id,
                OrderId = orderDetail.OrderId,
                ProductId = orderDetail.ProductId,
                ProductName = orderDetail.Product?.Name ?? string.Empty,
                Quantity = orderDetail.Quantity,
                UnitPrice = orderDetail.UnitPrice,
                SubTotal = orderDetail.SubTotal,
                Notes = orderDetail.Notes,
                Status = orderDetail.Status,
                LastModifiedByEmployeeId = orderDetail.LastModifiedByEmployeeId,
                LastModifiedByEmployeeName = orderDetail.LastModifiedByEmployee == null
                    ? null
                    : $"{orderDetail.LastModifiedByEmployee.Name} {orderDetail.LastModifiedByEmployee.LastName}".Trim()
            };
        }
    }
}