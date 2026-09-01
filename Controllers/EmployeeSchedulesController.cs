using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.EmployeeSchedules.DTOs;
using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Shared.Constants;
using System.Security.Claims;

namespace SaborExpress.Controllers
{
    [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
    [ApiController]
    [Route("api/employee-schedules")]
    public class EmployeeSchedulesController : ControllerBase
    {
        private readonly IEmployeeScheduleService _service;

        public EmployeeSchedulesController(IEmployeeScheduleService service) => _service = service;

        [HttpGet("employee/{employeeId:int}")]
        public async Task<IActionResult> GetByEmployee(int employeeId) =>
            Ok(await _service.GetByEmployeeAsync(employeeId, ObtenerUsuarioActualId()));
        [HttpGet("branch/{branchId}")] 
        public async Task<IActionResult> GetByBranch(int branchId) =>
            Ok(await _service.GetByBranchAsync(branchId, ObtenerUsuarioActualId()));


        [HttpGet("branch/{branchId:int}/today")]
        public async Task<IActionResult> GetByBranchToday(int branchId) =>
            Ok(await _service.GetByBranchTodayAsync(branchId, ObtenerUsuarioActualId()));

        // Nuevo: cualquier usuario autenticado puede consultar el turno activo
        // de un empleado ahora mismo (no solo Gerente/Administrador)
        [HttpGet("employee/{employeeId:int}/current")]
        [Authorize]
        public async Task<IActionResult> GetCurrentShift(int employeeId) =>
            Ok(await _service.GetCurrentShiftAsync(employeeId));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeScheduleDto dto)
        {
            var currentUserId = ObtenerUsuarioActualId();
            var result = await _service.CreateAsync(dto, currentUserId);
            return StatusCode(201, result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeScheduleDto dto)
        {
            var currentUserId = ObtenerUsuarioActualId();
            return Ok(await _service.UpdateAsync(id, dto, currentUserId));
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateScheduleStatusDto dto)
        {
            var currentUserId = ObtenerUsuarioActualId();
            return Ok(await _service.UpdateStatusAsync(id, dto, currentUserId));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = ObtenerUsuarioActualId();
            await _service.DeleteAsync(id, currentUserId);
            return NoContent();
        }

        private int ObtenerUsuarioActualId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        
    }
}