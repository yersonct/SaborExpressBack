using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Configurations.Models;

namespace SaborExpress.Data.Configurations
{
    public class BranchOperationalSettingsConfiguration : IEntityTypeConfiguration<BranchOperationalSettings>
    {
        public void Configure(EntityTypeBuilder<BranchOperationalSettings> builder)
        {
            builder.ToTable("branch_operational_settings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            builder.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.BranchId).IsUnique();

            builder.Property(x => x.OpeningTime).HasColumnName("opening_time").HasColumnType("time").IsRequired();
            builder.Property(x => x.ClosingTime).HasColumnName("closing_time").HasColumnType("time").IsRequired();

            builder.Property(x => x.TaxRate).HasColumnName("tax_rate").HasColumnType("decimal(5,2)").IsRequired();
            builder.Property(x => x.DeliveryFee).HasColumnName("delivery_fee").HasColumnType("decimal(12,2)").IsRequired();
            builder.Property(x => x.SuggestedTipPercent).HasColumnName("suggested_tip_percent").HasColumnType("decimal(5,2)").IsRequired();
            builder.Property(x => x.DeliveryRadiusKm).HasColumnName("delivery_radius_km").HasColumnType("decimal(5,2)").IsRequired();
            builder.Property(x => x.MinOrderAmount).HasColumnName("min_order_amount").HasColumnType("decimal(12,2)").IsRequired();

            builder.Property(x => x.AcceptsDelivery).HasColumnName("accepts_delivery").IsRequired();
            builder.Property(x => x.AcceptsDineIn).HasColumnName("accepts_dine_in").IsRequired();

            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        }
    }
}