// Modules/Payments/DTOs/CashierShiftSummaryDto.cs
namespace SaborExpress.Modules.Payments.DTOs
{
    // Resumen para el modal de "Cuadre de Caja" del Cajero — solo sus propios pagos de hoy.
    public class CashierShiftSummaryDto
    {
        public decimal TotalCash { get; set; }
        public decimal TotalCard { get; set; }
        public decimal TotalTransfer { get; set; }
        public decimal TotalAmount { get; set; }
        public int PaymentsCount { get; set; }
        public List<PaymentResponseDto> Payments { get; set; } = new();
    }
}