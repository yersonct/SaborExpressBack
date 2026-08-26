// Data/Configurations/OrderStatusHistoryConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Data.Configurations
{
    public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
        {
            builder.ToTable("order_status_history");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.OrderId)
                .HasColumnName("order_id")
                .IsRequired();

            builder.HasIndex(x => x.OrderId);

            builder.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.ChangedByEmployeeId)
                .HasColumnName("changed_by_employee_id")
                .IsRequired();

            builder.HasOne(x => x.ChangedByEmployee)
                .WithMany()
                .HasForeignKey(x => x.ChangedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.Notes)
                .HasColumnName("notes")
                .HasMaxLength(255);

            builder.Property(x => x.ChangedAt)
                .HasColumnName("changed_at")
                .IsRequired();
        }
    }
}