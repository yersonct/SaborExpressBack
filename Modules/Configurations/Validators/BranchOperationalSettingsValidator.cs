using System.Globalization;
using SaborExpress.Modules.Configurations.DTOs;

namespace SaborExpress.Modules.Configurations.Validators
{
    public class BranchOperationalSettingsValidator
    {
        public static TimeOnly ParseTime(string value, string field)
        {
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
                throw new ArgumentException("El IVA debe estar entre 0 y 100.");

            if (dto.SuggestedTipPercent is < 0 or > 100)
                throw new ArgumentException("La propina sugerida debe estar entre 0 y 100.");

            if (dto.DeliveryFee < 0)
                throw new ArgumentException("La tarifa de domicilio no puede ser negativa.");

            if (dto.MinOrderAmount < 0)
                throw new ArgumentException("El pedido mínimo no puede ser negativo.");

            if (dto.DeliveryRadiusKm is <= 0 or > 100)
                throw new ArgumentException("El radio de entrega debe estar entre 0.01 y 100 km.");

            if (!dto.AcceptsDelivery && !dto.AcceptsDineIn)
                throw new ArgumentException("La sede debe aceptar al menos domicilios o consumo en sitio.");
        }
    }
}