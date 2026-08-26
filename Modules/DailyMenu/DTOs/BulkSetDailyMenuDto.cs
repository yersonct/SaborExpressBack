using System;
using System.Collections.Generic;
using SaborExpress.Modules.DailyMenu.Enum;

namespace SaborExpress.Modules.DailyMenu.DTOs
{
    // Reemplaza de una vez todo el menú de una sucursal + fecha + franja
    // con la lista de productos indicada (borra lo anterior y crea lo nuevo).
    public class BulkSetDailyMenuDto
    {
        public int BranchId { get; set; }
        public DateTime Date { get; set; }
        public MealPeriod MealPeriod { get; set; }
        public List<int> ProductIds { get; set; } = new();
    }
}
