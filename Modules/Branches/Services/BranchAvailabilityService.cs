using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Shared.Helpers;

namespace SaborExpress.Modules.Branches.Services;

// Una fuente de verdad para el mapa y para aceptar pedidos nuevos.
// El horario siempre se interpreta en America/Bogota, no en la hora del VPS.
public sealed class BranchAvailabilityService
{
    private readonly AppDbContext _db;
    public BranchAvailabilityService(AppDbContext db) => _db = db;

    public sealed record Result(
        bool IsActive,
        bool IsOpen,
        bool HasSchedule,
        string OpeningTime,
        string ClosingTime,
        string UnavailableReason);

    public async Task<Result> GetAsync(int branchId)
    {
        var branch = await _db.Branches.AsNoTracking()
            .Where(b => b.Id == branchId)
            .Select(b => new { b.Id, b.Status })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException("La sede no existe.");

        var settings = await _db.BranchOperationalSettings.AsNoTracking()
            .Where(s => s.BranchId == branchId)
            .Select(s => new { s.OpeningTime, s.ClosingTime })
            .FirstOrDefaultAsync();
        var hasSchedule = settings is not null && settings.OpeningTime != settings.ClosingTime;
        var opening = hasSchedule ? settings!.OpeningTime.ToString("HH:mm") : string.Empty;
        var closing = hasSchedule ? settings!.ClosingTime.ToString("HH:mm") : string.Empty;

        if (!branch.Status)
            return new Result(false, false, hasSchedule, opening, closing, "Esta sede está temporalmente desactivada.");
        if (!hasSchedule)
            return new Result(true, false, false, string.Empty, string.Empty,
                "Esta sede aún no tiene un horario de atención configurado.");

        var now = ColombiaTime.Now;
        var currentTime = TimeOnly.FromDateTime(now);
        var openingTime = settings!.OpeningTime;
        var closingTime = settings.ClosingTime;
        var overnight = openingTime > closingTime;
        // En el cierre de las 02:00, el tramo 00:00-01:59 sigue
        // perteneciendo a la jornada del día anterior.
        var businessDate = DateOnly.FromDateTime(now);
        if (overnight && currentTime < closingTime)
            businessDate = businessDate.AddDays(-1);

        var withinHours = overnight
            ? currentTime >= openingTime || currentTime < closingTime
            : currentTime >= openingTime && currentTime < closingTime;
        if (!withinHours)
            return new Result(true, false, true, opening, closing,
                $"La sede está cerrada. Atiende de {opening} a {closing} (hora de Colombia).");

        // Un cierre real de caja impide reabrir pedidos en la misma jornada,
        // aunque una sesión cliente permanezca activa.
        var dayClosed = await _db.CashClosures.AsNoTracking()
            .AnyAsync(c => c.BranchId == branchId && c.ShiftDate == businessDate && c.ReopenedAtUtc == null);
        if (dayClosed)
            return new Result(true, false, true, opening, closing,
                "La sede ya realizó el cierre de caja de esta jornada.");

        return new Result(true, true, true, opening, closing, string.Empty);
    }

    public async Task EnsureCanAcceptNewOrderAsync(int branchId)
    {
        var status = await GetAsync(branchId);
        if (!status.IsOpen)
            throw new InvalidOperationException(status.UnavailableReason);
    }
}
