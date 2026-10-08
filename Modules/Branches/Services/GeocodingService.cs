using System.Globalization;
using System.Text.Json;
using SaborExpress.Modules.Branches.Interfaces;

namespace SaborExpress.Modules.Branches.Services
{
    // Usa Nominatim (OpenStreetMap), gratis y sin llave de API.
    // Política de uso: máximo ~1 solicitud por segundo y un User-Agent
    // identificable — ambos ya cumplidos aquí.
    public class GeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeocodingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "SaborExpress/1.0 (contacto@saborexpress.com)");
        }

        public async Task<(decimal Latitude, decimal Longitude)> GeocodeAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new InvalidOperationException("La dirección no puede estar vacía.");

            // Contexto de ciudad/departamento configurable, para no tener que
            // escribirlo en cada dirección (ej: "Calle 7f 12-43" -> se busca
            // como "Calle 7f 12-43, Neiva, Huila, Colombia").
            var cityContext = _configuration["Geocoding:DefaultCityContext"]
                ?? "Neiva, Huila, Colombia";

            var query = Uri.EscapeDataString($"{address}, {cityContext}");
            var url = $"https://nominatim.openstreetmap.org/search?format=json&limit=1&q={query}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    "No se pudo contactar el servicio de geolocalización. Intenta de nuevo.");

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);

            if (doc.RootElement.GetArrayLength() == 0)
                throw new InvalidOperationException(
                    $"No se pudo ubicar la dirección \"{address}\". Verifica que esté bien escrita " +
                    "(incluye tipo de vía, número y complemento).");

            var first = doc.RootElement[0];
            var lat = decimal.Parse(first.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
            var lon = decimal.Parse(first.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);

            return (lat, lon);
        }
    }
}