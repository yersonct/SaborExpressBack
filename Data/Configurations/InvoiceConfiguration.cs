using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Invoices.Models;

namespace SaborExpress.Data.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            // A payment may produce only one invoice, including under concurrent retries.
            builder.HasIndex(x => x.PaymentId).IsUnique();
            builder.HasIndex(x => x.InvoiceNumber).IsUnique();

            builder.Property(x => x.TaxRate)
                .HasColumnName("tax_rate")
                .HasColumnType("decimal(5,2)")
                .IsRequired();

            builder.Property(x => x.DeliveryFee)
                .HasColumnName("delivery_fee")
                .HasColumnType("decimal(12,2)")
                .IsRequired();

            builder.Property(x => x.TaxIncludedInPrice)
                .HasColumnName("tax_included_in_price")
                .IsRequired();
        }
    }
}
