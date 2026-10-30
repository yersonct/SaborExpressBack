using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Helpers;

namespace SaborExpress.Shared.Middleware;

/// <summary>
/// Cinturón de seguridad para sesiones existentes que no recibieron ForceLogout
/// (socket desconectado, cliente antiguo o JWT todavía vigente).
/// </summary>
public sealed class ClosedBranchGuardMiddleware
{
    private readonly RequestDelegate _next;
    public ClosedBranchGuardMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var user = context.User;
        var route = context.Request.Path;
        if (route.StartsWithSegments("/api/v1", out var remainder))
            route = new PathString("/api").Add(remainder);
        if (user.Identity?.IsAuthenticated == true
            && route.StartsWithSegments("/api")
            && !route.StartsWithSegments("/api/auth")
            && !user.IsInRole(RoleNames.Gerente)
            && !user.IsInRole(RoleNames.Administrador)
            && int.TryParse(user.FindFirst("EmployeeId")?.Value, out var employeeId))
        {
            var today = DateOnly.FromDateTime(ColombiaTime.Now);
            var blocked = await db.Employees.AsNoTracking()
                .Where(e => e.Id == employeeId && e.BranchId.HasValue)
                .AnyAsync(e => db.CashClosures.Any(c =>
                    c.BranchId == e.BranchId!.Value && c.ShiftDate == today && c.ReopenedAtUtc == null));
            if (blocked)
            {
                context.Response.StatusCode = StatusCodes.Status423Locked;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "BRANCH_CLOSED",
                    message = "La jornada de hoy terminó. Tu próxima entrada será en un turno de otro día."
                });
                return;
            }
        }
        await _next(context);
    }
}
