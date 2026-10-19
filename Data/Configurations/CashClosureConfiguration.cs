using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.CashClosures.Models;

namespace SaborExpress.Data.Configurations;

public sealed class CashClosureConfiguration : IEntityTypeConfiguration<CashClosure>
{
    public void Configure(EntityTypeBuilder<CashClosure> builder)
    {
        builder.ToTable("cash_closures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ScheduleId).HasColumnName("schedule_id");
        builder.Property(x => x.CashierId).HasColumnName("cashier_id");
        builder.Property(x => x.BranchId).HasColumnName("branch_id");
        builder.Property(x => x.ShiftDate).HasColumnName("shift_date");
        builder.Property(x => x.ShiftStartedAtUtc).HasColumnName("shift_started_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ClosedAtUtc).HasColumnName("closed_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CashExpected).HasColumnName("cash_expected").HasColumnType("decimal(14,2)");
        builder.Property(x => x.CashCounted).HasColumnName("cash_counted").HasColumnType("decimal(14,2)");
        builder.Property(x => x.CashDifference).HasColumnName("cash_difference").HasColumnType("decimal(14,2)");
        builder.Property(x => x.CardTotal).HasColumnName("card_total").HasColumnType("decimal(14,2)");
        builder.Property(x => x.TransferTotal).HasColumnName("transfer_total").HasColumnType("decimal(14,2)");
        builder.Property(x => x.WompiTotal).HasColumnName("wompi_total").HasColumnType("decimal(14,2)");
        builder.Property(x => x.TotalSales).HasColumnName("total_sales").HasColumnType("decimal(14,2)");
        builder.Property(x => x.PaymentsCount).HasColumnName("payments_count");
        builder.Property(x => x.CashierName).HasColumnName("cashier_name").HasMaxLength(250).IsRequired();
        builder.Property(x => x.BranchName).HasColumnName("branch_name").HasMaxLength(250).IsRequired();
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(x => x.PaymentsSnapshotJson).HasColumnName("payments_snapshot_json").HasColumnType("text").IsRequired();
        builder.Property(x => x.ReopenedAtUtc).HasColumnName("reopened_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReopenedByUserId).HasColumnName("reopened_by_user_id");
        builder.Property(x => x.ReopenReason).HasColumnName("reopen_reason").HasMaxLength(500);
        builder.HasIndex(x => x.ScheduleId).IsUnique().HasFilter("reopened_at_utc IS NULL"); // Solo un cierre vigente por turno.
        builder.HasIndex(x => new { x.BranchId, x.ShiftDate });
        builder.HasOne(x => x.Schedule).WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Cashier).WithMany().HasForeignKey(x => x.CashierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
