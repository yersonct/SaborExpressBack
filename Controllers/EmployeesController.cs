using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Employees.DTOs;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Shared.Constants;
using System.Security.Claims;

namespace SaborExpress.Controllers
{
    [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string estado = "activo")
        {
            var currentUserId = ObtenerUsuarioActualId();
            return Ok(await _employeeService.GetAllAsync(currentUserId, estado));
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = ObtenerUsuarioActualId();
            return Ok(await _employeeService.GetByIdAsync(id, currentUserId));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateEmployeeDto dto)
        {
            var currentUserId = ObtenerUsuarioActualId();
            var result = await _employeeService.CreateAsync(dto, currentUserId);

        return StatusCode(201, new
        {
            employeeId = result.Id,
            message = $"Empleado registrado correctamente. Se envio un codigo de activacion a {result.Email} para que confirme su cuenta."
        });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateEmployeeDto dto)
        {
            var currentUserId = ObtenerUsuarioActualId();
            return Ok(await _employeeService.UpdateAsync(id, dto, currentUserId));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUserId = ObtenerUsuarioActualId();
            await _employeeService.DeactivateAsync(id, currentUserId);
            return NoContent();
        }
        
        [HttpGet("{id}/cv")]
        public async Task<IActionResult> DownloadCv(int id)
        {
            var cv = await _employeeService.GetCvAsync(id);
            if (cv is null)
                return NotFound(new { error = "Este empleado no tiene CV cargado" });

            return File(cv.Value.Archivo, cv.Value.ContentType, cv.Value.NombreArchivo);
        }
        private int ObtenerUsuarioActualId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}