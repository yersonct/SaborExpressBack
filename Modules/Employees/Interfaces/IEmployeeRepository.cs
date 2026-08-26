using SaborExpress.Modules.Employees.Models;

namespace SaborExpress.Modules.Employees.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<Employee?> GetByIdAsync(int id);
        Task<Employee?> GetByDocumentAsync(string document);
        Task AddAsync(Employee employee);
        Task UpdateAsync(Employee employee);
        Task<bool> ExistsByDocumentAsync(string document);

        // Listados livianos: no traen CvFile, usar siempre para listar
        Task<List<EmployeeListItem>> GetAllLightAsync(string estado = "activo");
        Task<List<EmployeeListItem>> GetAllByBranchLightAsync(int branchId, string estado = "activo");

          Task<List<int>> GetAdminUserIdsByBranchAsync(int branchId);
    }
}