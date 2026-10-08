namespace SaborExpress.Modules.Branches.Interfaces
{
    public interface IGeocodingService
    {
        // Devuelve (latitud, longitud) a partir de una dirección en texto.
        // Lanza InvalidOperationException si no logra ubicarla.
        Task<(decimal Latitude, decimal Longitude)> GeocodeAsync(string address);
    }
}