// Modules/Tables/Mappings/TableMapper.cs
using SaborExpress.Modules.Tables.DTOs;
using SaborExpress.Modules.Tables.Models;

namespace SaborExpress.Modules.Tables.Mappings
{
    public static class TableMapper
    {
        public static TableResponseDto ToResponse(Table table)
        {
            return new TableResponseDto
            {
                Id = table.Id,
                BranchId = table.BranchId,
                BranchName = table.Branch?.Name,
                Number = table.Number,
                Status = table.Status,
                CreatedAt = table.CreatedAt
            };
        }
    }
}