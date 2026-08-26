using System;
using SaborExpress.Modules.DailyMenu.DTOs;

namespace SaborExpress.Modules.DailyMenu.Validators
{
    public class DailyMenuValidator
    {
        public void ValidateCreate(CreateDailyMenuItemDto dto)
        {
            if (dto.BranchId <= 0)
                throw new ArgumentException("Debe indicar una sucursal válida.");

            if (dto.ProductId <= 0)
                throw new ArgumentException("Debe indicar un producto válido.");

            ValidateDate(dto.Date);
        }

        public void ValidateUpdate(UpdateDailyMenuItemDto dto)
        {
            ValidateDate(dto.Date);
        }

        public void ValidateBulk(BulkSetDailyMenuDto dto)
        {
            if (dto.BranchId <= 0)
                throw new ArgumentException("Debe indicar una sucursal válida.");

            ValidateDate(dto.Date);

            if (dto.ProductIds == null || dto.ProductIds.Count == 0)
                throw new ArgumentException("Debe indicar al menos un producto para el menú del día.");

            if (dto.ProductIds.Distinct().Count() != dto.ProductIds.Count)
                throw new ArgumentException("La lista de productos no puede tener duplicados.");
        }

        private static void ValidateDate(DateTime date)
        {
            if (date == default)
                throw new ArgumentException("Debe indicar una fecha válida.");
        }
    }
}
