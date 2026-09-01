using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Payments.Models;

namespace SaborExpress.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("payments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.OrderId)
                .HasColumnName("order_id")
                .IsRequired();

            builder.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ahora opcional: un pago de Wompi no tiene cajero que lo registre
            builder.Property(x => x.CashierId)
                .HasColumnName("cashier_id")
                .IsRequired(false);

            builder.HasOne(x => x.Cashier)
                .WithMany()
                .HasForeignKey(x => x.CashierId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Property(x => x.Method)
                .HasColumnName("method")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.Amount)
                .HasColumnName("amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.PaidAt)
                .HasColumnName("paid_at")
                .IsRequired();

            // Nuevo: referencia propia que mandamos a Wompi
            builder.Property(x => x.WompiReference)
                .HasColumnName("wompi_reference")
                .HasMaxLength(100);

            builder.HasIndex(x => x.WompiReference)
                .IsUnique()
                .HasFilter("wompi_reference IS NOT NULL");

            // Nuevo: id de transacción que Wompi devuelve
            builder.Property(x => x.WompiTransactionId)
                .HasColumnName("wompi_transaction_id")
                .HasMaxLength(100);
        }
    }
}