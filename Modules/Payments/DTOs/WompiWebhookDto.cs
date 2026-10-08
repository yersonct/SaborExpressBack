// Modules/Payments/DTOs/WompiWebhookDto.cs
using System.Text.Json.Serialization;

namespace SaborExpress.Modules.Payments.DTOs
{
    public class WompiWebhookDto
    {
        public string Event { get; set; } = string.Empty;
        public WompiWebhookDataDto Data { get; set; } = new();
        // Se quitó "Signature": la firma se valida con el JSON crudo en WompiClient
    }

    public class WompiWebhookDataDto
    {
        public WompiTransactionDto Transaction { get; set; } = new();
    }

    public class WompiTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("amount_in_cents")]
        public int AmountInCents { get; set; }
    }
}