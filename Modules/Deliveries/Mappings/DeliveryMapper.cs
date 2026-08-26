// Modules/Deliveries/Mappings/DeliveryMapper.cs
using SaborExpress.Modules.Deliveries.DTOs;
using SaborExpress.Modules.Deliveries.Models;

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
                AddressText = delivery.Address?.AddressLine, // CORREGIDO
                DeliveryPersonId = delivery.DeliveryPersonId,
                DeliveryPersonName = delivery.DeliveryPerson == null
                    ? null
                    : $"{delivery.DeliveryPerson.Name} {delivery.DeliveryPerson.LastName}".Trim(),
                Status = delivery.Status,
                AssignedAt = delivery.AssignedAt,
                DeliveredAt = delivery.DeliveredAt
            };
        }
    }
}