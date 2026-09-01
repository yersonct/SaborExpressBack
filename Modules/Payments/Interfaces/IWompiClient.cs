// Modules/Payments/Interfaces/IWompiClient.cs
namespace SaborExpress.Modules.Payments.Interfaces
{
    public interface IWompiClient
    {
        string BuildIntegritySignature(string reference, int amountInCents, string currency);
        bool VerifyWebhookSignature(string rawBody, string signatureHeader);
        string PublicKey { get; }
    }
}