using System;
using SaborExpress.Modules.DailyMenu.Enum;

namespace SaborExpress.Modules.DailyMenu.DTOs
{
    public class UpdateDailyMenuItemDto
    {
        public DateTime Date { get; set; }
        public MealPeriod MealPeriod { get; set; }
        public bool IsAvailable { get; set; }
    }
}
