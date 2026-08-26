using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Data.Configurations
{
    public class OrderDetailHistoryConfiguration : IEntityTypeConfiguration<OrderDetailHistory>
    {
        public void Configure(EntityTypeBuilder<OrderDetailHistory> builder)
        {
            builder.ToTable("order_detail_history");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.OrderDetailId)
                .HasColumnName("order_detail_id")
                .IsRequired();

            builder.HasOne(x => x.OrderDetail)
                .WithMany(x => x.Histories)
                .HasForeignKey(x => x.OrderDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.ChangedByEmployeeId)
                .HasColumnName("changed_by_employee_id")
                .IsRequired();

            builder.HasOne(x => x.ChangedByEmployee)
                .WithMany()
                .HasForeignKey(x => x.ChangedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Action)
                .HasColumnName("action")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.OldValue)
                .HasColumnName("old_value")
                .HasColumnType("text");

            builder.Property(x => x.NewValue)
                .HasColumnName("new_value")
                .HasColumnType("text");

            builder.Property(x => x.Reason)
                .HasColumnName("reason")
                .HasMaxLength(255);

            builder.Property(x => x.ChangedAt)
                .HasColumnName("changed_at")
                .IsRequired();
        }
    }
}