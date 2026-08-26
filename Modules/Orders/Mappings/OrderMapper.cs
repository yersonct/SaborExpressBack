// Modules/Orders/Mappings/OrderMapper.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Mappings
{
    public static class OrderMapper
    {
        public static OrderResponseDto ToResponse(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name, // ajusta si tu Customer no tiene "Name"
                EmployeeId = order.EmployeeId,
                EmployeeName = order.Employee == null
                    ? null
                    : $"{order.Employee.Name} {order.Employee.LastName}".Trim(),
                TableId = order.TableId,
                TableNumber = order.Table?.Number,
                BranchId = order.BranchId,
                BranchName = order.Branch?.Name,
                OrderType = order.OrderType,
                Status = order.Status,
                SubTotal = order.SubTotal,
                Tax = order.Tax,
                Total = order.Total,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                OrderDetails = order.OrderDetails
                    .Select(OrderDetailMapper.ToResponse)
                    .ToList()
            };
        }

        public static OrderSummaryDto ToSummary(Order order)
        {
            return new OrderSummaryDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name,
                EmployeeId = order.EmployeeId,
                EmployeeName = order.Employee == null
                    ? null
                    : $"{order.Employee.Name} {order.Employee.LastName}".Trim(),
                TableId = order.TableId,
                TableNumber = order.Table?.Number,
                BranchId = order.BranchId,
                OrderType = order.OrderType,
                Status = order.Status,
                Total = order.Total,
                CreatedAt = order.CreatedAt
            };
        }
    }
}