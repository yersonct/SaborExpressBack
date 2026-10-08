// Modules/Payments/Interfaces/IWompiClient.cs
using SaborExpress.Modules.Payments.DTOs;

namespace SaborExpress.Modules.Payments.Interfaces
{
    public interface IWompiClient
    {
        string BuildIntegritySignature(string reference, int amountInCents, string currency);
        bool VerifyWebhookSignature(string rawBody, string signatureHeader);
        string PublicKey { get; }

        Task<WompiTransactionDto?> GetTransactionByReferenceAsync(string reference);
    }
}