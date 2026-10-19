using SaborExpress.Modules.EmployeeSchedules.Models;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Branches.Models;

namespace SaborExpress.Modules.CashClosures.Models;

// Registro inmutable: los importes y el listado de pagos se capturan al cerrar.
public sealed class CashClosure
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public EmployeeSchedule Schedule { get; set; } = null!;
    public int CashierId { get; set; }
    public Employee Cashier { get; set; } = null!;
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public DateOnly ShiftDate { get; set; }
    public DateTime ShiftStartedAtUtc { get; set; }
    public DateTime ClosedAtUtc { get; set; }
    public decimal CashExpected { get; set; }
    public decimal CashCounted { get; set; }
    public decimal CashDifference { get; set; }
    public decimal CardTotal { get; set; }
    public decimal TransferTotal { get; set; }
    public decimal WompiTotal { get; set; }
    public decimal TotalSales { get; set; }
    public int PaymentsCount { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string PaymentsSnapshotJson { get; set; } = "[]";
    // El arqueo y sus pagos nunca se sustituyen. La reapertura se registra
    // como evento administrativo; el siguiente cierre genera otro registro.
    public DateTime? ReopenedAtUtc { get; set; }
    public int? ReopenedByUserId { get; set; }
    public string? ReopenReason { get; set; }
}
