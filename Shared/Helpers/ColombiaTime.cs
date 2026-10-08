using System;

namespace SaborExpress.Shared.Helpers
{
    // Centraliza "la hora de ahora" en zona horaria de Colombia, para que no
    // dependa de en qué zona horaria esté configurado el sistema operativo
    // del servidor (en la nube casi siempre corre en UTC).
    public static class ColombiaTime
    {
        private static readonly TimeZoneInfo Zone = GetColombiaTimeZone();

        private static TimeZoneInfo GetColombiaTimeZone()
        {
            // El id del nombre cambia entre Windows y Linux, probamos ambos.
            try { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); }
            catch (TimeZoneNotFoundException)
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"); }
                catch (TimeZoneNotFoundException)
                {
                    // Último recurso: UTC-5 fijo (Colombia no tiene horario de verano)
                    return TimeZoneInfo.CreateCustomTimeZone("Colombia", TimeSpan.FromHours(-5), "Colombia", "Colombia");
                }
            }
        }

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
    }
}