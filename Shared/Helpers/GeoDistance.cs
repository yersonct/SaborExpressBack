namespace SaborExpress.Shared.Helpers
{
    public static class GeoDistance
    {
        // Distancia en km entre dos puntos (fórmula de Haversine)
        public static double Km(double lat1, double lon1, double lat2, double lon2)
        {
            const double r = 6371;
            double ToRad(double d) => d * Math.PI / 180;
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}