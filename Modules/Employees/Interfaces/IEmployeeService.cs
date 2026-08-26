using SaborExpress.Modules.Employees.DTOs;

namespace SaborExpress.Modules.Employees.Interfaces
{
    public interface IEmployeeService
    {
        Task<List<EmployeeResponseDto>> GetAllAsync(int currentUserId, string estado = "activo");
        Task<EmployeeResponseDto> GetByIdAsync(int id, int currentUserId);
        Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto dto, int currentUserId);
        Task<EmployeeResponseDto> UpdateAsync(int id, UpdateEmployeeDto dto, int currentUserId);
        Task DeactivateAsync(int id, int currentUserId);
        Task<(byte[] Archivo, string NombreArchivo, string ContentType)?> GetCvAsync(int id);
    }
}