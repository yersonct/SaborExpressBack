using System;
using SaborExpress.Modules.DailyMenu.Enum;

namespace SaborExpress.Modules.DailyMenu.DTOs
{
    public class CreateDailyMenuItemDto
    {
        public int BranchId { get; set; }
        public int ProductId { get; set; }
        public DateTime Date { get; set; }
        public MealPeriod MealPeriod { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
