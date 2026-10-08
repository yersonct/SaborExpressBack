// Modules/Payments/Services/WompiClient.cs
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SaborExpress.Modules.Payments.DTOs;
using SaborExpress.Modules.Payments.Interfaces;

namespace SaborExpress.Modules.Payments.Services
{
    public class WompiClient : IWompiClient
    {
        private readonly HttpClient _http;
        private readonly string _publicKey;
        private readonly string _privateKey;
        private readonly string _integritySecret;
        private readonly string _eventsSecret;

        public WompiClient(HttpClient http, IConfiguration configuration)
        {
            _http = http;

            // Sandbox por defecto. En producción: Wompi__BaseUrl = https://production.wompi.co/v1/
            var baseUrl = configuration["Wompi:BaseUrl"] ?? "https://sandbox.wompi.co/v1/";
            _http.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");

            _publicKey = (configuration["Wompi:PublicKey"]
                ?? throw new InvalidOperationException("Falta Wompi:PublicKey en la configuración")).Trim();
            _privateKey = (configuration["Wompi:PrivateKey"]
                ?? throw new InvalidOperationException("Falta Wompi:PrivateKey en la configuración")).Trim();
            _integritySecret = (configuration["Wompi:IntegritySecret"]
                ?? throw new InvalidOperationException("Falta Wompi:IntegritySecret en la configuración")).Trim();
            _eventsSecret = (configuration["Wompi:EventsSecret"]
                ?? throw new InvalidOperationException("Falta Wompi:EventsSecret en la configuración")).Trim();

            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _privateKey);
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
            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;

                var signature = root.GetProperty("signature");
                var checksum = signature.GetProperty("checksum").GetString();
                if (string.IsNullOrWhiteSpace(checksum))
                    checksum = signatureHeader;
                if (string.IsNullOrWhiteSpace(checksum))
                    return false;

                var timestamp = root.GetProperty("timestamp").ToString();

                var sb = new StringBuilder();
                foreach (var prop in signature.GetProperty("properties").EnumerateArray())
                {
                    var current = root.GetProperty("data");
                    foreach (var part in prop.GetString()!.Split('.'))
                        current = current.GetProperty(part);
                    sb.Append(current.ToString());
                }
                sb.Append(timestamp);
                sb.Append(_eventsSecret);

                var computed = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()))
                ).ToLower();

                return computed == checksum.ToLower();
            }
            catch
            {
                return false;
            }
        }

                // Consulta el estado real de la transacción en Wompi (respaldo del webhook)
        public async Task<WompiTransactionDto?> GetTransactionByReferenceAsync(string reference)
        {
            var res = await _http.GetAsync($"transactions?reference={Uri.EscapeDataString(reference)}");
            var body = await res.Content.ReadAsStringAsync();
            Console.WriteLine($"[WOMPI] GET {_http.BaseAddress}transactions?reference={reference} -> {(int)res.StatusCode} | {(body.Length > 300 ? body[..300] : body)}");

            if (!res.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array ||
                data.GetArrayLength() == 0)
                return null;

            // Si hubo varios intentos con la misma referencia, preferimos el aprobado
            JsonElement chosen = data[0];
            foreach (var t in data.EnumerateArray())
            {
                if (t.GetProperty("status").GetString() == "APPROVED")
                {
                    chosen = t;
                    break;
                }
            }

            return new WompiTransactionDto
            {
                Id = chosen.GetProperty("id").GetString() ?? string.Empty,
                Reference = chosen.GetProperty("reference").GetString() ?? string.Empty,
                Status = chosen.GetProperty("status").GetString() ?? string.Empty,
                AmountInCents = chosen.GetProperty("amount_in_cents").GetInt32()
            };
        }
    }
}