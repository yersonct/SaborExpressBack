using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.RolePermissions.DTOs;
using SaborExpress.Modules.RolePermissions.Interfaces;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
    [ApiController]
    [Route("api/role-permissions")]
    public class RolePermissionsController : ControllerBase
    {
        private readonly IRolePermissionService _service;

        public RolePermissionsController(IRolePermissionService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] CreateRolePermissionDto dto) =>
            StatusCode(201, await _service.AssignAsync(dto));

        [HttpDelete("{roleId}/{permissionId}")]
        public async Task<IActionResult> Remove(int roleId, int permissionId)
        {
            await _service.RemoveAsync(roleId, permissionId);
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _service.GetAllAsync());

        [HttpGet("role/{roleId}")]
        public async Task<IActionResult> GetByRole(int roleId) =>
            Ok(await _service.GetByRoleIdAsync(roleId));
    }
}