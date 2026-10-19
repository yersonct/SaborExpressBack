using System.ComponentModel.DataAnnotations;

namespace SaborExpress.Modules.CashClosures.DTOs;

public sealed class CreateCashClosureDto
{
    [Required(ErrorMessage = "Ingresa el efectivo contado antes de cerrar.")]
    [Range(
    typeof(decimal),
    "0",
    "999999999999.99",
    ParseLimitsInInvariantCulture = true,
    ErrorMessage = "El efectivo contado debe ser un valor válido no negativo."
)]
    public decimal? CashCounted { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public sealed class ReopenCashClosureDto
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;
}

public sealed record CashClosurePaymentDto(int PaymentId, int OrderId, string Method, decimal Amount, DateTime PaidAtUtc);

public sealed class CashClosurePreviewDto
{
    public int ScheduleId { get; init; }
    public int BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public DateOnly ShiftDate { get; init; }
    public bool IsClosed { get; init; }
    public int? ExistingClosureId { get; init; }
    public decimal CashExpected { get; init; }
    public decimal CardTotal { get; init; }
    public decimal TransferTotal { get; init; }
    public decimal WompiTotal { get; init; }
    public decimal TotalSales { get; init; }
    public int PaymentsCount { get; init; }
}

public sealed class CashClosureResponseDto
{
    public int Id { get; init; }
    public int ScheduleId { get; init; }
    public int CashierId { get; init; }
    public string CashierName { get; init; } = string.Empty;
    public int BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public DateOnly ShiftDate { get; init; }
    public DateTime ShiftStartedAtUtc { get; init; }
    public DateTime ClosedAtUtc { get; init; }
    public decimal CashExpected { get; init; }
    public decimal CashCounted { get; init; }
    public decimal CashDifference { get; init; }
    public decimal CardTotal { get; init; }
    public decimal TransferTotal { get; init; }
    public decimal WompiTotal { get; init; }
    public decimal TotalSales { get; init; }
    public int PaymentsCount { get; init; }
    public string? Notes { get; init; }
    public List<CashClosurePaymentDto> Payments { get; init; } = new();
    public DateTime? ReopenedAtUtc { get; init; }
    public int? ReopenedByUserId { get; init; }
    public string? ReopenReason { get; init; }
}
