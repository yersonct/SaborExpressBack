// Data/Configurations/OrderConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("orders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.CustomerId)
                .HasColumnName("customer_id");

            builder.HasIndex(x => x.CustomerId);

            builder.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.EmployeeId)
                .HasColumnName("employee_id");

            builder.HasIndex(x => x.EmployeeId);

            builder.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.TableId)
                .HasColumnName("table_id");
            builder.Property(x => x.Channel)
                .HasColumnName("channel")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            builder.HasIndex(x => x.TableId);

            builder.HasOne(x => x.Table)
                .WithMany()
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.BranchId)
                .HasColumnName("branch_id")
                .IsRequired();

            builder.HasIndex(x => x.BranchId);

            builder.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.OrderType)
                .HasColumnName("order_type")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.HasIndex(x => x.Status);

            builder.Property(x => x.SubTotal)
                .HasColumnName("sub_total")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            builder.Property(x => x.Tax)
                .HasColumnName("tax")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            builder.Property(x => x.Total)
                .HasColumnName("total")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            builder.Property(x => x.Notes)
                .HasColumnName("notes")
                .HasMaxLength(500);

            builder.Property(x => x.GuestName)
                .HasColumnName("guest_name")
                .HasMaxLength(150);

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");
        }
    }
}