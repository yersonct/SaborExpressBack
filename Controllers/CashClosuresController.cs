using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.CashClosures.DTOs;
using SaborExpress.Modules.CashClosures.Services;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;
using System.Security.Claims;

namespace SaborExpress.Controllers;

[ApiController]
[Authorize]
[Route("api/CashClosures")]
public sealed class CashClosuresController : ControllerBase
{
    private readonly CashClosureService _service;
    private readonly AppDbContext _db;
    public CashClosuresController(CashClosureService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    [HttpGet("current")]
    [Authorize(Roles = RoleNames.Cajero)]
    public async Task<ActionResult<CashClosurePreviewDto>> Current()
        => Ok(await _service.PreviewAsync(this.GetCurrentEmployeeId()));

    [HttpPost]
    [Authorize(Roles = RoleNames.Cajero)]
    public async Task<ActionResult<CashClosureResponseDto>> Close([FromBody] CreateCashClosureDto dto)
    {
        try
        {
            var closure = await _service.CloseAsync(this.GetCurrentEmployeeId(), dto);
            return Ok(closure);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{closureId:int}/reopen")]
    [Authorize(Roles = RoleNames.Gerente + "," + RoleNames.Administrador)]
    public async Task<ActionResult<CashClosureResponseDto>> Reopen(int closureId, [FromBody] ReopenCashClosureDto dto)
    {
        if (dto is null) return BadRequest(new { message = "Indica el motivo de la reapertura." });
        var actorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        try
        {
            return Ok(await _service.ReopenAsync(closureId, actorId, User.IsInRole(RoleNames.Gerente), dto.Reason));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Gerente + "," + RoleNames.Administrador)]
    public async Task<ActionResult<List<CashClosureResponseDto>>> History(
        [FromQuery] int? branchId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate)
    {
        var isManager = User.IsInRole(RoleNames.Gerente);
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        int? adminBranch = null;
        if (!isManager)
        {
            adminBranch = await _db.Employees.AsNoTracking()
                .Where(e => e.UserId == userId).Select(e => e.BranchId).FirstOrDefaultAsync();
            if (!adminBranch.HasValue || (branchId.HasValue && branchId != adminBranch))
                return Forbid();
        }
        try
        {
            return Ok(await _service.HistoryAsync(branchId, fromDate, toDate, isManager, adminBranch));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
