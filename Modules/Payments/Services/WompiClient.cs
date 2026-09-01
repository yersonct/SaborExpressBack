// Modules/Payments/Services/WompiClient.cs
using System.Security.Cryptography;
using System.Text;
using SaborExpress.Modules.Payments.Interfaces;

namespace SaborExpress.Modules.Payments.Services
{
    public class WompiClient : IWompiClient
    {
        private readonly string _publicKey;
        private readonly string _integritySecret;
        private readonly string _eventsSecret;

        public WompiClient(IConfiguration configuration)
        {
            _publicKey = configuration["Wompi:PublicKey"]
                ?? throw new InvalidOperationException("Falta Wompi:PublicKey en la configuración");
            _integritySecret = configuration["Wompi:IntegritySecret"]
                ?? throw new InvalidOperationException("Falta Wompi:IntegritySecret en la configuración");
            _eventsSecret = configuration["Wompi:EventsSecret"]
                ?? throw new InvalidOperationException("Falta Wompi:EventsSecret en la configuración");
        }

        public string PublicKey => _publicKey;

        // Wompi exige un hash SHA256 de: referencia + monto_en_centavos + moneda + secreto_integridad
        public string BuildIntegritySignature(string reference, int amountInCents, string currency)
        {
            var raw = $"{reference}{amountInCents}{currency}{_integritySecret}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLower();
        }

        // Valida que el webhook realmente venga de Wompi, no de un tercero
        public bool VerifyWebhookSignature(string rawBody, string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader))
                return false;

            var computed = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(rawBody + _eventsSecret))
            ).ToLower();

            return computed == signatureHeader.ToLower();
        }
    }
}