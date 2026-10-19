using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Interfaces;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SaborExpress.Controllers;

public sealed class EmailScheduleRangeRequest
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

[ApiController]
[Route("api/employee-schedules")]
[Authorize(Roles = RoleNames.Gerente + "," + RoleNames.Administrador)]
public sealed class EmployeeScheduleEmailsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly INotificationService _mail;
    public EmployeeScheduleEmailsController(AppDbContext db, INotificationService mail)
    { _db = db; _mail = mail; }

    [HttpPost("branch/{branchId:int}/email-week")]
    public async Task<IActionResult> SendWeekByBranch(int branchId, [FromBody] EmailScheduleRangeRequest request)
    {
        if (branchId <= 0 || request is null || request.FromDate == default || request.ToDate == default
            || request.ToDate.DayNumber - request.FromDate.DayNumber != 6)
            return BadRequest(new { message = "Selecciona una semana válida de siete días." });

        // Un administrador únicamente puede enviar horarios de su sede.
        if (!User.IsInRole(RoleNames.Gerente))
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actorId))
                return Unauthorized();
            var ownBranchId = await _db.Employees.AsNoTracking()
                .Where(e => e.UserId == actorId).Select(e => e.BranchId).FirstOrDefaultAsync();
            if (ownBranchId != branchId) return Forbid();
        }

        if (!await _db.Branches.AsNoTracking().AnyAsync(b => b.Id == branchId))
            return NotFound(new { message = "La sede no existe." });

        var shifts = await _db.EmployeeSchedules.AsNoTracking()
            .Include(s => s.Employee).ThenInclude(e => e.User)
            .Include(s => s.Role).Include(s => s.Branch)
            .Where(s => s.BranchId == branchId
                && s.ShiftDate >= request.FromDate && s.ShiftDate <= request.ToDate
                && s.Status != SaborExpress.Modules.EmployeeSchedules.Enum.ScheduleStatus.Ausente)
            .OrderBy(s => s.EmployeeId).ThenBy(s => s.ShiftDate).ThenBy(s => s.StartTime)
            .ToListAsync();

        var employees = shifts.GroupBy(s => s.EmployeeId).ToList();
        if (employees.Count == 0)
            return BadRequest(new { message = "No hay empleados con turnos asignados en la semana seleccionada." });

        static string Escape(string? value) => HtmlEncoder.Default.Encode(value ?? "");
        var sent = 0;
        var skipped = 0;
        var failed = 0;
        foreach (var grouped in employees)
        {
            var employee = grouped.First().Employee;
            if (string.IsNullOrWhiteSpace(employee.User?.Email))
            {
                skipped++;
                continue;
            }

            var fullname = Escape($"{employee.Name} {employee.LastName}".Trim());
            var rows = string.Join("", grouped.Select(s =>
                $"<tr><td style='padding:8px;border-bottom:1px solid #ddd'>{s.ShiftDate:dd/MM/yyyy}</td>" +
                $"<td style='padding:8px;border-bottom:1px solid #ddd'>{Escape(s.Role.Name)}</td>" +
                $"<td style='padding:8px;border-bottom:1px solid #ddd'>{s.StartTime.ToString("HH:mm")} - {s.EndTime.ToString("HH:mm")}</td>" +
                $"<td style='padding:8px;border-bottom:1px solid #ddd'>{Escape(s.Branch.Name)}</td></tr>"));
            var body = $"<p>Hola, <strong>{fullname}</strong>. Estos son tus turnos de SaborExpress " +
                $"del {request.FromDate:dd/MM/yyyy} al {request.ToDate:dd/MM/yyyy}:</p>" +
                "<table style='border-collapse:collapse;width:100%'>" +
                "<thead><tr><th>Fecha</th><th>Rol</th><th>Hora (Colombia)</th><th>Sede</th></tr></thead>" +
                $"<tbody>{rows}</tbody></table><p>Si hay algún error, comunícate con la administración.</p>";
            try
            {
                await _mail.SendEmailAsync(employee.User.Email!,
                    $"SaborExpress - Turnos {request.FromDate:dd/MM/yyyy} a {request.ToDate:dd/MM/yyyy}", body);
                sent++;
            }
            catch (Exception)
            {
                // Un correo fallido no impide enviar los turnos de los demás.
                failed++;
            }
        }
        return Ok(new { sent, skipped, failed, employeesWithShifts = employees.Count });
    }

    [HttpPost("employee/{employeeId:int}/email")]
    public async Task<IActionResult> SendSchedule(int employeeId,
        [FromBody] EmailScheduleRangeRequest request)
    {
        if (request.FromDate == default || request.ToDate == default
            || request.ToDate < request.FromDate
            || request.ToDate.DayNumber - request.FromDate.DayNumber > 6)
            return BadRequest(new { message = "Selecciona un día o hasta siete días consecutivos." });

        var employee = await _db.Employees.AsNoTracking().Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == employeeId);
        if (employee is null) return NotFound(new { message = "Empleado inexistente." });

        // Un Administrador solo envía correo a empleados de SU sede.
        if (!User.IsInRole(RoleNames.Gerente))
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var ownBranch = await _db.Employees.AsNoTracking()
                .Where(e => e.UserId == userId).Select(e => e.BranchId).FirstOrDefaultAsync();
            if (!ownBranch.HasValue || employee.BranchId != ownBranch) return Forbid();
        }

        if (string.IsNullOrWhiteSpace(employee.User.Email))
            return BadRequest(new { message = "El empleado no tiene correo registrado." });
        var shifts = await _db.EmployeeSchedules.AsNoTracking()
            .Include(s => s.Role).Include(s => s.Branch)
            .Where(s => s.EmployeeId == employeeId
                && s.ShiftDate >= request.FromDate && s.ShiftDate <= request.ToDate)
            .OrderBy(s => s.ShiftDate).ThenBy(s => s.StartTime)
            .ToListAsync();
        if (shifts.Count == 0)
            return BadRequest(new { message = "El empleado no tiene turnos en las fechas seleccionadas." });

        if (!User.IsInRole(RoleNames.Gerente))
        {
            var actorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var ownBranch = await _db.Employees.AsNoTracking()
                .Where(e => e.UserId == actorId).Select(e => e.BranchId).FirstOrDefaultAsync();
            if (shifts.Any(s => s.BranchId != ownBranch)) return Forbid();
        }

        static string Escape(string? input) => HtmlEncoder.Default.Encode(input ?? string.Empty);
        var rows = string.Join("", shifts.Select(s =>
            $"<tr><td style='padding:8px;border-bottom:1px solid #ddd'>{s.ShiftDate:dd/MM/yyyy}</td>" +
            $"<td style='padding:8px;border-bottom:1px solid #ddd'>{Escape(s.Role.Name)}</td>" +
            $"<td style='padding:8px;border-bottom:1px solid #ddd'>{s.StartTime.ToString("HH:mm")}–{s.EndTime.ToString("HH:mm")}</td>" +
            $"<td style='padding:8px;border-bottom:1px solid #ddd'>{Escape(s.Branch.Name)}</td>" +
            $"<td style='padding:8px;border-bottom:1px solid #ddd'>{Escape(s.Status.ToString())}</td></tr>"));
        var fullname = Escape($"{employee.Name} {employee.LastName}".Trim());
        var subject = $"Turnos asignados - {request.FromDate:dd/MM/yyyy} a {request.ToDate:dd/MM/yyyy}";
        var html = $"<p>Hola, <b>{fullname}</b>. Estos son tus turnos programados en SaborExpress:</p>" +
            "<table style='border-collapse:collapse;width:100%'>" +
            "<thead><tr><th>Fecha</th><th>Rol</th><th>Horario (Colombia)</th><th>Sede</th><th>Estado</th></tr></thead>" +
            $"<tbody>{rows}</tbody></table><p>Si observas una diferencia, contacta al administrador.</p>";
        try
        {
            await _mail.SendEmailAsync(employee.User.Email, subject, html);
            return Ok(new { message = "Correo enviado.", employeeId, email = employee.User.Email, shifts = shifts.Count });
        }
        catch (Exception)
        {
            return StatusCode(502, new { message = "No se pudo enviar el correo. Revisa la configuración de Brevo e inténtalo de nuevo." });
        }
    }
}
