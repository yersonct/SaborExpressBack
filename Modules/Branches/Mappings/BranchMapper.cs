using SaborExpress.Modules.Branches.DTOs;
using SaborExpress.Modules.Branches.Models;

namespace SaborExpress.Modules.Branches.Mappings
{
    public static class BranchMapper
    {
        public static BranchResponseDto ToResponse(BranchSummary summary) => new()
        {
            Id = summary.Id,
            Name = summary.Name,
            Address = summary.Address,
            Phone = summary.Phone,
            Status = summary.Status,
            EmployeeCount = summary.EmployeeCount
        };

        // Usado solo justo después de crear/actualizar, cuando ya tenemos
        // la entidad en memoria y el conteo de empleados por separado.
        public static BranchResponseDto ToResponse(Branch branch, int employeeCount) => new()
        {
            Id = branch.Id,
            Name = branch.Name,
            Address = branch.Address,
            Phone = branch.Phone,
            Status = branch.Status,
            EmployeeCount = employeeCount
        };
    }
}