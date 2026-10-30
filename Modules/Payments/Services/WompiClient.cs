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
                ?? throw new SaborExpress.Shared.Exceptions.ConflictException("Falta Wompi:PublicKey en la configuración")).Trim();
            _privateKey = (configuration["Wompi:PrivateKey"]
                ?? throw new SaborExpress.Shared.Exceptions.ConflictException("Falta Wompi:PrivateKey en la configuración")).Trim();
            _integritySecret = (configuration["Wompi:IntegritySecret"]
                ?? throw new SaborExpress.Shared.Exceptions.ConflictException("Falta Wompi:IntegritySecret en la configuración")).Trim();
            _eventsSecret = (configuration["Wompi:EventsSecret"]
                ?? throw new SaborExpress.Shared.Exceptions.ConflictException("Falta Wompi:EventsSecret en la configuración")).Trim();

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
                // Wompi puede enviar el checksum en el cuerpo, en el header o en ambos.
                // Si están ambos presentes, deben coincidir. Comparar hashes en tiempo constante.
                string? checksum = signature.TryGetProperty("checksum", out var bodyChecksum)
                    ? bodyChecksum.GetString()
                    : null;
                var header = signatureHeader?.Trim();
                checksum = string.IsNullOrWhiteSpace(checksum) ? header : checksum.Trim();
                if (string.IsNullOrWhiteSpace(checksum))
                    return false;

                var received = Convert.FromHexString(checksum);
                if (received.Length != 32)
                    return false;

                if (!string.IsNullOrWhiteSpace(header))
                {
                    var fromHeader = Convert.FromHexString(header);
                    if (fromHeader.Length != 32 ||
                        !CryptographicOperations.FixedTimeEquals(received, fromHeader))
                        return false;
                }

                if (!root.TryGetProperty("timestamp", out var timestampElement) ||
                    timestampElement.ValueKind != JsonValueKind.Number ||
                    !timestampElement.TryGetInt64(out var timestampValue))
                    return false;

                if (!signature.TryGetProperty("properties", out var properties) ||
                    properties.ValueKind != JsonValueKind.Array || properties.GetArrayLength() == 0 ||
                    !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                    return false;

                var sb = new StringBuilder();
                foreach (var prop in properties.EnumerateArray())
                {
                    if (prop.ValueKind != JsonValueKind.String)
                        return false;

                    var path = prop.GetString();
                    if (string.IsNullOrWhiteSpace(path))
                        return false;

                    var current = data;
                    foreach (var part in path.Split('.'))
                    {
                        if (string.IsNullOrWhiteSpace(part) || current.ValueKind != JsonValueKind.Object ||
                            !current.TryGetProperty(part, out var next))
                            return false;
                        current = next;
                    }

                    if (current.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                        return false;
                    sb.Append(current.ToString());
                }
                sb.Append(timestampValue);
                sb.Append(_eventsSecret);

                var computed = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
                return CryptographicOperations.FixedTimeEquals(computed, received);
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
            // No registrar el cuerpo de Wompi ni referencias de pago en consola:
            // pueden contener información financiera que acabaría en los logs.

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
                Currency = chosen.TryGetProperty("currency", out var currency)
                    ? currency.GetString() ?? string.Empty
                    : string.Empty,
                AmountInCents = chosen.GetProperty("amount_in_cents").GetInt32()
            };
        }
    }
}
