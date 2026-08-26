// Modules/Tables/Models/Table.cs
using SaborExpress.Modules.Branches.Models;
using SaborExpress.Modules.Tables.Enum;

namespace SaborExpress.Modules.Tables.Models
{
    public class Table
    {
        public int Id { get; set; }

        public int BranchId { get; set; } // FK
        public Branch Branch { get; set; } = null!;

        public int Number { get; set; }
        public TableStatus Status { get; set; } = TableStatus.Available;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}