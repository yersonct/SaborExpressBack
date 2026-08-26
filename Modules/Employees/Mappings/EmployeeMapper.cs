using SaborExpress.Modules.Employees.DTOs;
using SaborExpress.Modules.Employees.Models;

namespace SaborExpress.Modules.Employees.Mappings
{
    public static class EmployeeMapper
    {
        public static EmployeeResponseDto ToResponse(Employee employee)
        {
            return new EmployeeResponseDto
            {
                Id = employee.Id,
                UserId = employee.UserId,
                Name = employee.Name,
                LastName = employee.LastName,
                Document = employee.Document,
                Email = employee.User?.Email,
                Phone = employee.Phone,
                Address = employee.Address,
                Photo = employee.Photo,
                RoleNames = employee.User?.UserRoles?
                    .Select(ur => ur.Role?.Name ?? string.Empty)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList() ?? new List<string>(),
                BranchId = employee.BranchId,
                Status = employee.Status,
                BasePay = employee.BasePay,
                HasCv = employee.CvFile != null && employee.CvFile.Length > 0
            };
        }

        public static EmployeeResponseDto ToResponse(EmployeeListItem item)
        {
            return new EmployeeResponseDto
            {
                Id = item.Id,
                UserId = item.UserId,
                Name = item.Name,
                LastName = item.LastName,
                Document = item.Document,
                Email = item.Email,
                Phone = item.Phone,
                Address = item.Address,
                Photo = item.Photo,
                RoleNames = item.RoleNames,
                BranchId = item.BranchId,
                Status = item.Status,
                BasePay = item.BasePay,
                HasCv = item.HasCv
            };
        }
    }
}