using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Permissions.DTOs;
using SaborExpress.Modules.Permissions.Interfaces;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _permissionService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) =>
            Ok(await _permissionService.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePermissionDto dto) =>
            StatusCode(201, await _permissionService.CreateAsync(dto));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _permissionService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePermissionDto dto) =>
            Ok(await _permissionService.UpdateAsync(id, dto));
    }
}