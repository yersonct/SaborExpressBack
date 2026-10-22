// Modules/Invoices/Mappings/InvoiceMapper.cs
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Invoices.Models;

namespace SaborExpress.Modules.Invoices.Mappings
{
    public static class InvoiceMapper
    {
        public static InvoiceResponseDto ToResponse(Invoice invoice)
        {
            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                PaymentId = invoice.PaymentId,
                OrderId = invoice.OrderId,
                BranchId = invoice.BranchId,
                BranchName = invoice.Branch?.Name,
                BranchAddress = invoice.Branch?.Address,
                BranchPhone = invoice.Branch?.Phone,
                CustomerName = invoice.Order?.Customer is { } customer
                    ? string.Join(" ", new[] { customer.Name, customer.LastName }
                        .Where(value => !string.IsNullOrWhiteSpace(value)))
                    : invoice.Order?.GuestName,
                CustomerDocument = invoice.Order?.Customer?.Document,
                CustomerEmail = invoice.Order?.Customer?.User?.Email,
                DeliveryAddressLine = invoice.Order?.Address?.AddressLine,
                DeliveryAddressReference = invoice.Order?.Address?.Reference,
                OrderType = invoice.Order?.OrderType.ToString(),
                TableNumber = invoice.Order?.Table?.Number,
                CashierName = invoice.Payment?.Cashier is { } cashier
                    ? string.Join(" ", new[] { cashier.Name, cashier.LastName }
                        .Where(value => !string.IsNullOrWhiteSpace(value)))
                    : null,
                PaymentMethod = invoice.Payment?.Method.ToString(),
                PaymentAmount = invoice.Payment?.Amount,
                PaidAt = invoice.Payment?.PaidAt,
                Items = invoice.Order?.OrderDetails?
                    .Where(item => item.Status != SaborExpress.Modules.Orders.Enum.OrderDetailStatus.Voided &&
                                   item.Status != SaborExpress.Modules.Orders.Enum.OrderDetailStatus.Cancelled)
                    .Select(item => new InvoiceItemResponseDto
                    {
                        Name = item.Product?.Name ?? string.Empty,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Total = item.SubTotal,
                        Notes = item.Notes,
                        IsToGo = item.IsToGo
                    })
                    .ToList() ?? new List<InvoiceItemResponseDto>(),
                SubTotal = invoice.SubTotal,
                Tax = invoice.Tax,
                TaxRate = invoice.TaxRate,
                TaxIncludedInPrice = invoice.TaxIncludedInPrice,
                DeliveryFee = invoice.DeliveryFee,
                Total = invoice.Total,
                IsVoided = invoice.IsVoided,
                IssuedAt = invoice.IssuedAt
            };
        }
    }
}
