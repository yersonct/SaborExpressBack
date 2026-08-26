using System;
using SaborExpress.Modules.Branches.Models;
using SaborExpress.Modules.Products.Models;
using SaborExpress.Modules.DailyMenu.Enum;

namespace SaborExpress.Modules.DailyMenu.Models
{
    public class DailyMenuItem
    {
        public int Id { get; set; }

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public DateTime Date { get; set; }
        public MealPeriod MealPeriod { get; set; }
        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
