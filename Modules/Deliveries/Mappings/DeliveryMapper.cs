// Modules/Deliveries/Mappings/DeliveryMapper.cs
using SaborExpress.Modules.Deliveries.DTOs;
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Orders.Enum; // NUEVO — para OrderDetailStatus

namespace SaborExpress.Modules.Deliveries.Mappings
{
    public static class DeliveryMapper
    {
        public static DeliveryResponseDto ToResponse(Delivery delivery)
        {
            return new DeliveryResponseDto
            {
                Id = delivery.Id,
                OrderId = delivery.OrderId,
                AddressId = delivery.AddressId,
                AddressText = delivery.Address?.AddressLine,
                AddressReference = delivery.Address?.Reference,
                AddressLatitude = delivery.Address?.Latitude,
                AddressLongitude = delivery.Address?.Longitude,
                DeliveryPersonId = delivery.DeliveryPersonId,
                DeliveryPersonName = delivery.DeliveryPerson == null
                    ? null
                    : $"{delivery.DeliveryPerson.Name} {delivery.DeliveryPerson.LastName}".Trim(),
                DeliveryPersonPhone = delivery.DeliveryPerson?.Phone,
                Status = delivery.Status,
                AssignedAt = delivery.AssignedAt,
                DeliveredAt = delivery.DeliveredAt,

                // NUEVO
                CustomerName = delivery.Order?.Customer != null
                    ? $"{delivery.Order.Customer.Name} {delivery.Order.Customer.LastName}".Trim()
                    : delivery.Order?.GuestName,
                CustomerPhone = delivery.Order?.Customer?.Phone,
                ItemsCount = delivery.Order?.OrderDetails
                    .Count(d => d.Status != OrderDetailStatus.Voided) ?? 0,
                PaymentMethod = delivery.PaymentMethod,
                PaymentStatus = delivery.PaymentStatus,

                // Misma regla que usa DeliveryService para dejar salir al repartidor.
                KitchenReady = delivery.Order != null
                    && !delivery.Order.OrderDetails.Any(d =>
                        d.Status != OrderDetailStatus.Voided
                        && d.Status != OrderDetailStatus.Cancelled
                        && d.Status != OrderDetailStatus.Ready
                        && d.Status != OrderDetailStatus.Delivered),

                // NUEVO
                OrderNotes = delivery.Order?.Notes,
                Total = delivery.Order?.Total ?? 0,
                AmountPaid = delivery.AmountPaid,
                AmountPending = Math.Max(0, (delivery.Order?.Total ?? 0) - delivery.AmountPaid),
                OrderStatus = delivery.Order?.Status.ToString(),
                CustomerRating = delivery.CustomerRating,
                CustomerRatingComment = delivery.CustomerRatingComment,
                Items = delivery.Order?.OrderDetails
                    .Where(d => d.Status != OrderDetailStatus.Voided
                             && d.Status != OrderDetailStatus.Cancelled)
                    .Select(d => new DeliveryItemDto
                    {
                        ProductName = d.Product?.Name ?? "Producto",
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        Notes = d.Notes
                    })
                    .ToList() ?? new List<DeliveryItemDto>()
            };
        }
    }
}