using System.Globalization;
using SaborExpress.Modules.Configurations.DTOs;

namespace SaborExpress.Modules.Configurations.Validators
{
    public class BranchOperationalSettingsValidator
    {
        public static TimeOnly ParseTime(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{field} es obligatoria.");

            if (!TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                throw new ArgumentException($"{field} debe tener formato HH:mm (ej. 08:30).");
            return time;
        }

        public void Validate(UpdateBranchOperationalSettingsDto dto)
        {
            var open = ParseTime(dto.OpeningTime, "La hora de apertura");
            var close = ParseTime(dto.ClosingTime, "La hora de cierre");

            // Se permite cerrar después de medianoche (ej. 18:00 -> 02:00), pero no igual.
            if (open == close)
                throw new ArgumentException("La hora de apertura y la de cierre no pueden ser iguales.");

            if (dto.TaxRate is < 0 or > 100)
                throw new ArgumentException("El porcentaje de Impoconsumo debe estar entre 0 y 100.");

            if (dto.DeliveryFee < 0)
                throw new ArgumentException("La tarifa de domicilio no puede ser negativa.");

            if (dto.DeliveryFee != decimal.Truncate(dto.DeliveryFee))
                throw new ArgumentException("La tarifa de domicilio debe ser un número entero.");

            if (dto.DeliveryRadiusKm is <= 0 or > 100)
                throw new ArgumentException("El radio de entrega debe estar entre 0.01 y 100 km.");

            if (dto.MaxActiveDeliveriesPerCourier is < 1 or > 20)
                throw new ArgumentException("La cantidad máxima de domicilios activos por repartidor debe estar entre 1 y 20.");
        }
    }
}
