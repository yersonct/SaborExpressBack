using SaborExpress.Modules.Branches.Models;

namespace SaborExpress.Modules.Branches.Interfaces
{
    public interface IBranchRepository
    {
        Task<bool> ExistsByNameAsync(string name);
        Task<bool> ExistsByNameAsync(string name, int excludeId);
        Task<bool> HasEmployeesAsync(int branchId);
        Task AddAsync(Branch branch);

        // Para editar/borrar: solo la entidad, sin cargar empleados (no se necesitan para mutar)
        Task<Branch?> GetByIdAsync(int id);
        Task UpdateAsync(Branch branch);
        Task DeleteAsync(Branch branch);

        // Para listados/lectura: proyección liviana con el conteo calculado en SQL
        Task<List<BranchSummary>> GetAllSummariesAsync();
        Task<BranchSummary?> GetSummaryByIdAsync(int id);
        Task<int> GetEmployeeCountAsync(int branchId);
    }
}