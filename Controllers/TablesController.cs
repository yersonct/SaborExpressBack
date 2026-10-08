// Controllers/TablesController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Tables.DTOs;
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/Tables")]
    public class TablesController : ControllerBase
    {
        private readonly ITableService _tableService;

        public TablesController(ITableService tableService)
        {
            _tableService = tableService;
        }

        [HttpGet("branch/{branchId:int}")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            var result = await _tableService.GetByBranchIdAsync(branchId);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _tableService.GetByIdAsync(id);
            return Ok(result);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTableDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _tableService.CreateAsync(dto, currentUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTableDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _tableService.UpdateAsync(id, dto, currentUserId);
            return Ok(result);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Mesero}")]
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTableStatusDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _tableService.UpdateStatusAsync(id, dto, currentUserId);
            return Ok(result);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = GetCurrentUserId();
            await _tableService.DeleteAsync(id, currentUserId);
            return NoContent();
        }
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Token inválido: no contiene el identificador del usuario.");

            return int.Parse(claim.Value);
        }
    }
}