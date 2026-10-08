namespace SaborExpress.Modules.Branches.Interfaces
{
    /// <summary>
    /// Responsabilidad única: decidir si un usuario puede operar sobre una sede puntual,
    /// y resolver cuál es "su" sede. Gerente = cualquier sede. Administrador = solo la suya.
    /// </summary>
    public interface IBranchAccessGuard
    {
        Task<int?> GetOwnBranchIdAsync(int userId);
        Task EnsureCanAccessBranchAsync(int branchId, int userId);
        Task<bool> IsGerenteAsync(int userId);
    }
}