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

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Mesero},{RoleNames.Cajero},{RoleNames.Cocinero},{RoleNames.AuxiliarCocina}")]
        [HttpGet("branch/{branchId:int}")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            var result = await _tableService.GetByBranchIdAsync(branchId, GetCurrentUserId());
            return Ok(result);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Mesero},{RoleNames.Cajero},{RoleNames.Cocinero},{RoleNames.AuxiliarCocina}")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _tableService.GetByIdAsync(id, GetCurrentUserId());
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

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Mesero},{RoleNames.Cajero}")]
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTableStatusDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _tableService.UpdateStatusAsync(id, dto, currentUserId);
            return Ok(result);
        }

        // Consultas y regularización separados de PATCH /status; no permitir a meseros.
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Cajero}")]
        [HttpGet("{id:int}/release-review")]
        public async Task<IActionResult> ReviewRelease(int id)
            => Ok(await _tableService.GetReleaseReviewAsync(id, GetCurrentUserId()));

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Cajero}")]
        [HttpGet("branch/{branchId:int}/previous-release-reviews")]
        public async Task<IActionResult> ReviewPreviousReleases(int branchId)
            => Ok(await _tableService.GetPreviousDayReleaseReviewsAsync(branchId, GetCurrentUserId()));

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Cajero}")]
        [HttpPost("{id:int}/release-previous")]
        public async Task<IActionResult> ReleasePrevious(int id, [FromBody] ReleasePreviousTableDto dto)
            => Ok(await _tableService.ReleasePreviousDayAsync(id, dto, GetCurrentUserId()));

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