using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Data;
using SaborExpress.Modules.CashClosures.DTOs;
using SaborExpress.Modules.CashClosures.Models;
using SaborExpress.Modules.EmployeeSchedules.Enum;
using SaborExpress.Modules.EmployeeSchedules.Models;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Helpers;

namespace SaborExpress.Modules.CashClosures.Services;

public sealed class CashClosureService
{
    private readonly AppDbContext _db;
    private readonly IRealtimeNotifier _realtime;
    private readonly ILogger<CashClosureService> _logger;
    public CashClosureService(AppDbContext db, IRealtimeNotifier realtime, ILogger<CashClosureService> logger)
    {
        _db = db;
        _realtime = realtime;
        _logger = logger;
    }

    private async Task<EmployeeSchedule> FindCurrentCashierShiftAsync(int cashierId)
    {
        var now = ColombiaTime.Now;
        var today = DateOnly.FromDateTime(now);
        var currentTime = now.TimeOfDay;
        var shifts = await _db.EmployeeSchedules
            .Include(s => s.Role)
            .Include(s => s.Branch)
            .Include(s => s.Employee)
            .Where(s => s.EmployeeId == cashierId && s.ShiftDate == today
                && s.Role.Name == RoleNames.Cajero && s.Status != ScheduleStatus.Ausente)
            .ToListAsync();

        // El cuadre puede realizarse durante el turno y hasta 2h despues del fin,
        // pero nunca antes de comenzar. Cada cierre pertenece a un schedule_id real.
        var selected = shifts
            .Where(s => currentTime >= s.StartTime.ToTimeSpan()
                && currentTime <= s.EndTime.ToTimeSpan().Add(TimeSpan.FromHours(2)))
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefault();
        if (selected is null)
            throw new SaborExpress.Shared.Exceptions.ConflictException("No tienes un turno de Cajero iniciado para cerrar hoy.");
        if (selected.Employee.Status != "Activo")
            throw new SaborExpress.Shared.Exceptions.ConflictException("El empleado no esta activo.");
        return selected;
    }

    private async Task<List<CashClosurePaymentDto>> LoadPaymentsAsync(EmployeeSchedule shift, DateTime closedAtUtc)
    {
        var startUtc = ColombiaTime.ToUtc(shift.ShiftDate.ToDateTime(shift.StartTime));
        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.CashierId == shift.EmployeeId && p.Order.BranchId == shift.BranchId
                && p.PaidAt >= startUtc && p.PaidAt <= closedAtUtc
                && p.Status == PaymentStatus.Completed)
            .OrderBy(p => p.PaidAt)
            .Select(p => new { p.Id, p.OrderId, p.Method, p.Amount, p.PaidAt })
            .ToListAsync();
        return payments.Select(p => new CashClosurePaymentDto(
            p.Id, p.OrderId, p.Method.ToString(), p.Amount, p.PaidAt)).ToList();
    }

    private static decimal SumMethod(IEnumerable<CashClosurePaymentDto> payments, string method)
        => payments.Where(p => p.Method == method).Sum(p => p.Amount);

    public async Task<CashClosurePreviewDto> PreviewAsync(int cashierId)
    {
        var shift = await FindCurrentCashierShiftAsync(cashierId);
        var existing = await _db.CashClosures.AsNoTracking().FirstOrDefaultAsync(c => c.ScheduleId == shift.Id && c.ReopenedAtUtc == null);
        if (existing != null)
        {
            return new CashClosurePreviewDto
            {
                ScheduleId = shift.Id, BranchId = existing.BranchId, BranchName = existing.BranchName,
                ShiftDate = existing.ShiftDate, IsClosed = true, ExistingClosureId = existing.Id,
                CashExpected = existing.CashExpected, CardTotal = existing.CardTotal,
                TransferTotal = existing.TransferTotal, WompiTotal = existing.WompiTotal,
                TotalSales = existing.TotalSales, PaymentsCount = existing.PaymentsCount
            };
        }
        if (shift.Status != ScheduleStatus.Programado)
            throw new SaborExpress.Shared.Exceptions.ConflictException("Este turno ya finalizo sin un cierre de caja; consulta al administrador.");
        var payments = await LoadPaymentsAsync(shift, DateTime.UtcNow);
        return new CashClosurePreviewDto
        {
            ScheduleId = shift.Id, BranchId = shift.BranchId, BranchName = shift.Branch.Name,
            ShiftDate = shift.ShiftDate, IsClosed = false,
            CashExpected = SumMethod(payments, nameof(PaymentMethod.Cash)),
            CardTotal = SumMethod(payments, nameof(PaymentMethod.Card)),
            TransferTotal = SumMethod(payments, nameof(PaymentMethod.Transfer)),
            WompiTotal = SumMethod(payments, nameof(PaymentMethod.Wompi)),
            TotalSales = payments.Sum(p => p.Amount), PaymentsCount = payments.Count
        };
    }

    public async Task<CashClosureResponseDto> CloseAsync(int cashierId, CreateCashClosureDto input)
    {
        if (input.CashCounted is not decimal countedCash || countedCash < 0 || countedCash > 999999999999.99m)
            throw new ArgumentException("Es obligatorio declarar el efectivo contado, incluso si es cero.");
        var notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        if (notes?.Length > 500) throw new ArgumentException("Observaciones demasiado largas (maximo 500 caracteres).");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var shift = await FindCurrentCashierShiftAsync(cashierId);
        // El advisory lock agrupa cajeros de la misma sede y el mismo día.
        // Así no pueden cerrar simultáneamente dos cajas de una jornada.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({shift.BranchId}, {shift.ShiftDate.DayNumber})");
        if (await _db.CashClosures.AnyAsync(c => c.BranchId == shift.BranchId && c.ShiftDate == shift.ShiftDate && c.ReopenedAtUtc == null))
            throw new SaborExpress.Shared.Exceptions.ConflictException("Esta sede ya tiene un cierre de caja para hoy.");
        // Bloqueo PostgreSQL por turno: dos solicitudes concurrentes no pueden
        // registrar cierres duplicados. La BD refuerza con indice UNIQUE.
        var lockedShift = (await _db.EmployeeSchedules
            .FromSqlInterpolated($"SELECT * FROM employee_schedules WHERE id = {shift.Id} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync()).Single();
        if (await _db.CashClosures.AnyAsync(c => c.ScheduleId == shift.Id && c.ReopenedAtUtc == null))
            throw new SaborExpress.Shared.Exceptions.ConflictException("Este turno ya tiene un cierre de caja registrado.");
        if (lockedShift.Status != ScheduleStatus.Programado)
            throw new SaborExpress.Shared.Exceptions.ConflictException("El turno ya fue finalizado; no se puede cerrar de nuevo.");

        var closedAtUtc = DateTime.UtcNow;
        // Un QR del propio cajero pendiente puede acreditarse DESPUES del cierre.
        // Se impide cerrar hasta que quede confirmado, fallido o cancelado.
        var startUtc = ColombiaTime.ToUtc(shift.ShiftDate.ToDateTime(shift.StartTime));
        var hasPendingWompi = await _db.Payments.AnyAsync(p =>
            p.CashierId == cashierId && p.Order.BranchId == shift.BranchId &&
            p.PaidAt >= startUtc && p.PaidAt <= closedAtUtc &&
            p.Method == PaymentMethod.Wompi && p.Status == PaymentStatus.Pending);
        if (hasPendingWompi)
            throw new SaborExpress.Shared.Exceptions.ConflictException("Hay cobros Wompi por QR pendientes. Confirma o resuelve esos pagos antes de cerrar la caja.");
        var payments = await LoadPaymentsAsync(shift, closedAtUtc);
        var cashExpected = SumMethod(payments, nameof(PaymentMethod.Cash));
        var closure = new CashClosure
        {
            ScheduleId = shift.Id, CashierId = shift.EmployeeId, BranchId = shift.BranchId,
            CashierName = $"{shift.Employee.Name} {shift.Employee.LastName}".Trim(),
            BranchName = shift.Branch.Name, ShiftDate = shift.ShiftDate,
            ShiftStartedAtUtc = ColombiaTime.ToUtc(shift.ShiftDate.ToDateTime(shift.StartTime)),
            ClosedAtUtc = closedAtUtc, CashExpected = cashExpected,
            CashCounted = countedCash, CashDifference = countedCash - cashExpected,
            CardTotal = SumMethod(payments, nameof(PaymentMethod.Card)),
            TransferTotal = SumMethod(payments, nameof(PaymentMethod.Transfer)),
            WompiTotal = SumMethod(payments, nameof(PaymentMethod.Wompi)),
            TotalSales = payments.Sum(p => p.Amount), PaymentsCount = payments.Count,
            Notes = notes, PaymentsSnapshotJson = JsonSerializer.Serialize(payments)
        };
        _db.CashClosures.Add(closure);
        // Al cerrar una caja se termina la jornada operativa de esta sede,
        // pero NUNCA se altera el turno de un Administrador o Gerente.
        var staffShifts = await _db.EmployeeSchedules.Include(s => s.Role)
            .Where(s => s.BranchId == shift.BranchId && s.ShiftDate == shift.ShiftDate
                && s.Status == ScheduleStatus.Programado
                && s.Role.Name != RoleNames.Gerente
                && s.Role.Name != RoleNames.Administrador)
            .ToListAsync();
        foreach (var staffShift in staffShifts)
        {
            staffShift.Status = ScheduleStatus.Cumplido;
            staffShift.UpdatedAt = closedAtUtc;
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        // El cierre ya fue confirmado en BD. Revocar sesiones y notificar
        // es una consecuencia: un fallo del socket no puede deshacer el cierre.
        try
        {
            var staffUserIds = await _db.EmployeeSchedules.AsNoTracking()
                .Where(s => s.BranchId == shift.BranchId && s.ShiftDate == shift.ShiftDate
                    && s.Role.Name != RoleNames.Gerente && s.Role.Name != RoleNames.Administrador
                    && !_db.UserRoles.Any(ur => ur.UserId == s.Employee.UserId
                        && (ur.Role.Name == RoleNames.Gerente || ur.Role.Name == RoleNames.Administrador)))
                .Select(s => s.Employee.UserId).Distinct().ToListAsync();
            if (staffUserIds.Count > 0)
            {
                await _db.RefreshTokens.Where(r => staffUserIds.Contains(r.UserId) && !r.IsRevoked)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.IsRevoked, true));
                foreach (var userId in staffUserIds)
                    await _realtime.ForceLogoutAsync(userId, "branch_closed");
            }
            await _realtime.AdminDataChangedAsync("CashClosures");
            await _realtime.PublicBranchesChangedAsync();
            await _realtime.AdminDataChangedAsync("EmployeeSchedules");
            await _realtime.OrdersChangedAsync();
            await _realtime.PaymentsChangedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Cierre guardado; la notificación de salida falló ({ErrorType}).", ex.GetType().Name);
        }
        return ToResponse(closure);
    }

    public async Task<CashClosureResponseDto> ReopenAsync(int closureId, int actorUserId, bool isManager, string reason)
    {
        var justification = reason?.Trim() ?? "";
        if (justification.Length < 10 || justification.Length > 500)
            throw new ArgumentException("Debes indicar el motivo de la reapertura (10 a 500 caracteres).");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var target = await _db.CashClosures.FirstOrDefaultAsync(c => c.Id == closureId)
            ?? throw new KeyNotFoundException("No existe este cierre de caja.");

        if (!isManager)
        {
            var administratorBranch = await _db.Employees.AsNoTracking()
                .Where(e => e.UserId == actorUserId).Select(e => e.BranchId).FirstOrDefaultAsync();
            if (administratorBranch is null || administratorBranch.Value != target.BranchId)
                throw new SaborExpress.Shared.Exceptions.ForbiddenException("Solo puedes reabrir la caja de tu sede.");
        }

        var today = DateOnly.FromDateTime(ColombiaTime.Now);
        if (target.ShiftDate != today)
            throw new SaborExpress.Shared.Exceptions.ConflictException("Solamente se puede reabrir un cierre de la jornada actual.");

        // Mismo candado usado por CloseAsync: impide carreras entre cerrar y reabrir
        // incluso cuando existan varios servidores conectados a PostgreSQL.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({target.BranchId}, {target.ShiftDate.DayNumber})");
        await _db.Entry(target).ReloadAsync();
        if (target.ReopenedAtUtc.HasValue)
            throw new SaborExpress.Shared.Exceptions.ConflictException("Este cierre ya fue reabierto.");

        var otherClosure = await _db.CashClosures.AnyAsync(c => c.Id != target.Id
            && c.BranchId == target.BranchId && c.ShiftDate == target.ShiftDate
            && c.ReopenedAtUtc == null);
        if (otherClosure)
            throw new SaborExpress.Shared.Exceptions.ConflictException("Existe otro cierre vigente para esta sede y jornada.");

        var openedAtUtc = DateTime.UtcNow;
        target.ReopenedAtUtc = openedAtUtc;
        target.ReopenedByUserId = actorUserId;
        target.ReopenReason = justification;

        // Fix24 finalizó todos los turnos operativos de la sede. Restaurar solo
        // aquellos que todavía están dentro de SU horario, no turnos vencidos
        // ni roles directivos. Nunca crear nuevos turnos.
        var now = TimeOnly.FromDateTime(ColombiaTime.Now);
        var shifts = await _db.EmployeeSchedules.Include(s => s.Role)
            .Where(s => s.BranchId == target.BranchId && s.ShiftDate == target.ShiftDate
                && s.Status == ScheduleStatus.Cumplido
                && s.UpdatedAt == target.ClosedAtUtc
                && s.Role.Name != RoleNames.Administrador && s.Role.Name != RoleNames.Gerente)
            .ToListAsync();
        foreach (var shift in shifts)
        {
            var inHours = shift.StartTime < shift.EndTime
                ? now >= shift.StartTime && now < shift.EndTime
                : now >= shift.StartTime || now < shift.EndTime;
            if (!inHours) continue;
            shift.Status = ScheduleStatus.Programado;
            shift.UpdatedAt = openedAtUtc;
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        // Se reactivan las reglas de login y los endpoints por el nuevo estado
        // persistido. Los empleados vuelven a entrar con credenciales propias.
        try
        {
            await _realtime.AdminDataChangedAsync("CashClosures");
            await _realtime.AdminDataChangedAsync("EmployeeSchedules");
            await _realtime.OrdersChangedAsync();
            await _realtime.PaymentsChangedAsync();
            await _realtime.PublicBranchesChangedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Reapertura guardada; no fue posible emitir todos los eventos ({ErrorType}).", ex.GetType().Name);
        }
        return ToResponse(target);
    }

    public async Task<List<CashClosureResponseDto>> HistoryAsync(int? branchId, DateOnly? fromDate, DateOnly? toDate,
        bool isManager, int? administratorBranchId)
    {
        if (!isManager)
        {
            if (administratorBranchId is null)
                throw new UnauthorizedAccessException("Administrador sin sede asignada.");
            if (branchId.HasValue && branchId.Value != administratorBranchId.Value)
                throw new SaborExpress.Shared.Exceptions.ForbiddenException("Solo puedes consultar cierres de tu sede.");
            branchId = administratorBranchId;
        }
        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            throw new ArgumentException("El rango de fechas es invalido.");
        if (fromDate.HasValue && toDate.HasValue && toDate.Value.DayNumber - fromDate.Value.DayNumber > 366)
            throw new ArgumentException("El historial se consulta por periodos de hasta 366 dias.");
        var query = _db.CashClosures.AsNoTracking().AsQueryable();
        if (branchId.HasValue) query = query.Where(c => c.BranchId == branchId.Value);
        if (fromDate.HasValue) query = query.Where(c => c.ShiftDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(c => c.ShiftDate <= toDate.Value);
        var closings = await query.OrderByDescending(c => c.ClosedAtUtc).Take(200).ToListAsync();
        return closings.Select(ToResponse).ToList();
    }

    private static CashClosureResponseDto ToResponse(CashClosure c) => new()
    {
        Id = c.Id, ScheduleId = c.ScheduleId, CashierId = c.CashierId,
        CashierName = c.CashierName, BranchId = c.BranchId, BranchName = c.BranchName,
        ShiftDate = c.ShiftDate, ShiftStartedAtUtc = c.ShiftStartedAtUtc, ClosedAtUtc = c.ClosedAtUtc,
        CashExpected = c.CashExpected, CashCounted = c.CashCounted,
        CashDifference = c.CashDifference, CardTotal = c.CardTotal,
        TransferTotal = c.TransferTotal, WompiTotal = c.WompiTotal,
        TotalSales = c.TotalSales, PaymentsCount = c.PaymentsCount, Notes = c.Notes,
        ReopenedAtUtc = c.ReopenedAtUtc, ReopenedByUserId = c.ReopenedByUserId, ReopenReason = c.ReopenReason,
        Payments = JsonSerializer.Deserialize<List<CashClosurePaymentDto>>(c.PaymentsSnapshotJson) ?? new()
    };
}
