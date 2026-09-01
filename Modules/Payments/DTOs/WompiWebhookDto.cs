// Modules/Payments/DTOs/WompiWebhookDto.cs
namespace SaborExpress.Modules.Payments.DTOs
{
    // Estructura simplificada del payload real de Wompi (evento "transaction.updated")
    public class WompiWebhookDto
    {
        public string Event { get; set; } = string.Empty;
        public WompiWebhookDataDto Data { get; set; } = new();
        public string Signature { get; set; } = string.Empty; // se valida aparte, viene en el header o el body según config
    }

    public class WompiWebhookDataDto
    {
        public WompiTransactionDto Transaction { get; set; } = new();
    }

    public class WompiTransactionDto
    {
        public string Id { get; set; } = string.Empty;         // WompiTransactionId
        public string Reference { get; set; } = string.Empty;  // nuestro WompiReference
        public string Status { get; set; } = string.Empty;     // "APPROVED", "DECLINED", "VOIDED", "ERROR"
        public int AmountInCents { get; set; }
    }
}