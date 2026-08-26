// Modules/Tables/DTOs/TableResponseDto.cs
using SaborExpress.Modules.Tables.Enum;

namespace SaborExpress.Modules.Tables.DTOs
{
    public class TableResponseDto
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public int Number { get; set; }
        public TableStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}