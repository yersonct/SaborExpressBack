using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.UserRoles.DTOs;
using SaborExpress.Modules.UserRoles.Interfaces;
using SaborExpress.Shared.Constants;
using System.Security.Claims;

namespace SaborExpress.Controllers
{
    [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
    [ApiController]
    [Route("api/user-roles")]
    public class UserRolesController : ControllerBase
    {
        private readonly IUserRoleService _service;

        public UserRolesController(IUserRoleService service) => _service = service;

        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] CreateUserRoleDto dto)
        {
            var currentUserId = ObtenerUsuarioActualId();
            var result = await _service.AssignAsync(dto, currentUserId);
            return StatusCode(201, result);
        }

        [HttpDelete("{userId}/{roleId}")]
        public async Task<IActionResult> Remove(int userId, int roleId)
        {
            var currentUserId = ObtenerUsuarioActualId();
            await _service.RemoveAsync(userId, roleId, currentUserId);
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId) => Ok(await _service.GetByUserIdAsync(userId));

        private int ObtenerUsuarioActualId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}