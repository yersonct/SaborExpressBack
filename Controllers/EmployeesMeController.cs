using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Employees.DTOs;
using SaborExpress.Modules.Employees.Interfaces;

namespace SaborExpress.Controllers
{
    [Authorize] // cualquier empleado autenticado, sin restricción de rol
    [ApiController]
    [Route("api/Employees")]
    public class EmployeesMeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesMeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var employeeId = ObtenerEmployeeIdActual();
            return Ok(await _employeeService.GetMeAsync(employeeId));
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateEmployeeMeDto dto)
        {
            var employeeId = ObtenerEmployeeIdActual();
            return Ok(await _employeeService.UpdateMeAsync(employeeId, dto));
        }

        [HttpPatch("me/availability")]
        public async Task<IActionResult> SetMyAvailability([FromBody] SetAvailabilityDto dto)
        {
            var employeeId = ObtenerEmployeeIdActual();
            return Ok(await _employeeService.SetMyAvailabilityAsync(employeeId, dto.IsAvailable));
        }

        private int ObtenerEmployeeIdActual()
        {
            var claim = User.FindFirst("EmployeeId")
                ?? throw new UnauthorizedAccessException("Este usuario no tiene un EmployeeId en su token.");
            return int.Parse(claim.Value);
        }
    }
}