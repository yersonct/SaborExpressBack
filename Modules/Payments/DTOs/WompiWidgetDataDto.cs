// Modules/Payments/DTOs/WompiWidgetDataDto.cs
namespace SaborExpress.Modules.Payments.DTOs
{
    // Lo que el backend devuelve para que el frontend arme el widget de Wompi
    public class WompiWidgetDataDto
    {
        public int PaymentId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal AmountInCents { get; set; } // Wompi trabaja en centavos
        public string Currency { get; set; } = "COP";
        public string PublicKey { get; set; } = string.Empty;
        public string IntegritySignature { get; set; } = string.Empty; // hash requerido por Wompi
    }
}