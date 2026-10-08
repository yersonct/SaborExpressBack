// Modules/Deliveries/DTOs/DeliveryResponseDto.cs
using SaborExpress.Modules.Deliveries.Enum;

namespace SaborExpress.Modules.Deliveries.DTOs
{
    public class DeliveryResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int AddressId { get; set; }
        public string? AddressText { get; set; }
        public string? AddressReference { get; set; }
        public decimal? AddressLatitude { get; set; }
        public decimal? AddressLongitude { get; set; }
        public int DeliveryPersonId { get; set; }
        public string? DeliveryPersonName { get; set; }
        public string? DeliveryPersonPhone { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // NUEVO
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public int ItemsCount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }

        // true cuando cocina terminó todos los platos y el repartidor puede salir.
        public bool KitchenReady { get; set; }

        // NUEVO: lo que el repartidor necesita ver del pedido del cliente
        public string? OrderNotes { get; set; }
        public decimal Total { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal AmountPending { get; set; }
        public string? OrderStatus { get; set; }

        // Calificación del cliente al repartidor (null si aún no califica)
        public int? CustomerRating { get; set; }
        public string? CustomerRatingComment { get; set; }

        public List<DeliveryItemDto> Items { get; set; } = new();
    }

    public class DeliveryItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
    }
}