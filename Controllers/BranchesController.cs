using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Branches.DTOs;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BranchesController : ControllerBase
    {
        private readonly IBranchService _branchService;

        public BranchesController(IBranchService branchService) => _branchService = branchService;

        [Authorize(Roles = RoleNames.Gerente)]
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _branchService.GetAllAsync());

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var currentUserId = GetCurrentUserId();
            return Ok(await _branchService.GetMineAsync(currentUserId));
        }

        [Authorize(Roles = RoleNames.Gerente)]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _branchService.GetByIdAsync(id));

        [Authorize(Roles = RoleNames.Gerente)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBranchDto dto) =>
            StatusCode(201, await _branchService.CreateAsync(dto));

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBranchDto dto)
        {
            var currentUserId = GetCurrentUserId();
            return Ok(await _branchService.UpdateAsync(id, dto, currentUserId));
        }

        [Authorize(Roles = RoleNames.Gerente)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _branchService.DeleteAsync(id);
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