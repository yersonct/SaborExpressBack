// Modules/Orders/Mappings/OrderMapper.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Payments.Enum;

namespace SaborExpress.Modules.Orders.Mappings
{
    public static class OrderMapper
    {
        // La suma de pagos completados determina si el pedido está pagado.
        // cuentan los pagos Completed, y el pedido está pagado cuando la suma
        // cubre el total (o lo supera, por redondeos).
        private static bool ComputeIsFullyPaid(Order order)
        {
            var totalPaid = order.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            return totalPaid >= order.Total;
        }
        public static OrderResponseDto ToResponse(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name ?? order.GuestName,
                EmployeeId = order.EmployeeId,
                EmployeeName = order.Employee == null
                    ? null
                    : $"{order.Employee.Name} {order.Employee.LastName}".Trim(),
                TableId = order.TableId,
                TableNumber = order.Table?.Number,
                TableSessionId = order.TableSessionId,
                BranchId = order.BranchId,
                BranchName = order.Branch?.Name,
                OrderType = order.OrderType,
                AssignedDeliveryPersonId = order.AssignedDeliveryPersonId,
                AssignedDeliveryPersonName = order.AssignedDeliveryPersonName,
                Status = order.Status,
                SubTotal = order.SubTotal,
              Tax = order.Tax,
                TaxRate = order.TaxRate,
                TaxIncludedInPrice = order.TaxIncludedInPrice,
                DeliveryFee = order.DeliveryFee,
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
                CustomerName = order.Customer?.Name ?? order.GuestName,
                EmployeeId = order.EmployeeId,
                EmployeeName = order.Employee == null
                    ? null
                    : $"{order.Employee.Name} {order.Employee.LastName}".Trim(),
                TableId = order.TableId,
                TableNumber = order.Table?.Number,
                BranchId = order.BranchId,
                Channel = order.Channel,
                OrderType = order.OrderType,
                AssignedDeliveryPersonId = order.AssignedDeliveryPersonId,
                AssignedDeliveryPersonName = order.AssignedDeliveryPersonName,
               Status = order.Status,
                SubTotal = order.SubTotal,
                Tax = order.Tax,
                TaxRate = order.TaxRate,
                TaxIncludedInPrice = order.TaxIncludedInPrice,
                DeliveryFee = order.DeliveryFee,
                Total = order.Total,
                IsFullyPaid = ComputeIsFullyPaid(order),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                PaidAt = order.Payments
                    .Where(payment => payment.Status == PaymentStatus.Completed)
                    .Select(payment => (DateTime?)payment.PaidAt)
                    .Max(),
                OrderDetails = order.OrderDetails
                    .Select(d => new OrderSummaryItemDto
                    {
                        Id = d.Id,
                        ProductId = d.ProductId,
                        ProductName = d.Product?.Name ?? string.Empty,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        Notes = d.Notes,
                        IsToGo = d.IsToGo,
                        BatchNumber = d.BatchNumber,
                        Status = d.Status
                    })
                    .ToList()
            };
        }
    }
}
