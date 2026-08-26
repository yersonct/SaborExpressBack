// Modules/Configurations/Models/BranchSetting.cs
using SaborExpress.Modules.Branches.Models;

namespace SaborExpress.Modules.Configurations.Models
{
    public class BranchSetting
    {
        public int Id { get; set; }

        public int? BranchId { get; set; } // null = configuración global
        public Branch? Branch { get; set; }

        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}