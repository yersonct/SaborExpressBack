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
        Task<Employee?> GetByUserIdAsync(int userId);

        // Listados livianos: no traen CvFile, usar siempre para listar
        Task<List<EmployeeListItem>> GetAllLightAsync(string estado = "activo");
        Task<List<EmployeeListItem>> GetAllByBranchLightAsync(int branchId, string estado = "activo");

          Task<List<int>> GetAdminUserIdsByBranchAsync(int branchId);

        // Genérico: UserIds de empleados activos de una sede que tengan
        // alguno de los roles indicados (ej. avisar a "Mesero" y "Cocinero"
        // cuando entra un pedido nuevo).
        Task<List<int>> GetUserIdsByBranchAndRolesAsync(int branchId, params string[] roleNames);
        Task<List<int>> GetUserIdsByRolesAsync(params string[] roleNames);
    }
}