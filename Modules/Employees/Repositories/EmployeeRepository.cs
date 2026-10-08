using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Shared.Constants; 

namespace SaborExpress.Modules.Employees.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly AppDbContext _context;

        public EmployeeRepository(AppDbContext context)
        {
            _context = context;
        }

        private IQueryable<Employee> EmployeesWithRelations()
        {
            return _context.Employees
                .Include(e => e.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .Include(e => e.Branch);
        }

        public async Task<Employee?> GetByIdAsync(int id)
            => await EmployeesWithRelations().FirstOrDefaultAsync(e => e.Id == id);

        public async Task<Employee?> GetByUserIdAsync(int userId)
            => await EmployeesWithRelations().FirstOrDefaultAsync(e => e.UserId == userId);

        public async Task<Employee?> GetByDocumentAsync(string document)
            => await EmployeesWithRelations().FirstOrDefaultAsync(e => e.Document == document);

        public async Task<List<EmployeeListItem>> GetAllLightAsync(string estado = "activo")
        {
            var query = _context.Employees.AsQueryable();
            query = AplicarFiltroEstado(query, estado);

            return await ProjectToListItem(query).ToListAsync();
        }

        public async Task<List<EmployeeListItem>> GetAllByBranchLightAsync(int branchId, string estado = "activo")
        {
            var query = _context.Employees
                .Where(e => e.BranchId == branchId)
                .AsQueryable();

            query = AplicarFiltroEstado(query, estado);

            return await ProjectToListItem(query).ToListAsync();
        }

        private static IQueryable<EmployeeListItem> ProjectToListItem(IQueryable<Employee> query)
        {
            return query.Select(e => new EmployeeListItem
            {
                Id = e.Id,
                UserId = e.UserId,
                Name = e.Name,
                LastName = e.LastName,
                Document = e.Document,
                Email = e.User.Email,
                Phone = e.Phone,
                Address = e.Address,
                RoleNames = e.User.UserRoles.Select(ur => ur.Role.Name).ToList(),
                BranchId = e.BranchId,
                Status = e.Status,
                BasePay = e.BasePay,
                HasCv = e.CvFile != null && e.CvFile.Length > 0
            });
        }

        private static IQueryable<Employee> AplicarFiltroEstado(IQueryable<Employee> query, string estado)
        {
            var estadoLimpio = estado.Trim().ToLower();
            return estadoLimpio switch
            {
                "activo" => query.Where(e => e.Status == "Activo"),
                "retirado" => query.Where(e => e.Status == "Retirado"),
                "todos" => query,
                _ => throw new ArgumentException($"El filtro '{estado}' no es valido. Usa: activo, retirado o todos.")
            };
        }

        public async Task AddAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByDocumentAsync(string document)
        {
            return await _context.Employees.AnyAsync(e => e.Document == document);
        }

        public async Task<List<int>> GetAdminUserIdsByBranchAsync(int branchId)
        {
            return await _context.Employees
                .Where(e => e.BranchId == branchId && e.Status != "Retirado")
                .SelectMany(e => e.User.UserRoles)
                .Where(ur => ur.Role.Name == RoleNames.Administrador)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<int>> GetUserIdsByBranchAndRolesAsync(int branchId, params string[] roleNames)
        {
            return await _context.Employees
                .Where(e => e.BranchId == branchId && e.Status != "Retirado")
                .SelectMany(e => e.User.UserRoles)
                .Where(ur => roleNames.Contains(ur.Role.Name))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync(); 
        }

        // Sin filtro de sede: usado para roles que ven TODAS las sedes (ej. Gerente).
        public async Task<List<int>> GetUserIdsByRolesAsync(params string[] roleNames)
        {
            return await _context.Employees
                .Where(e => e.Status != "Retirado")
                .SelectMany(e => e.User.UserRoles)
                .Where(ur => roleNames.Contains(ur.Role.Name))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();
        }
    }
}