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
            builder.Property(x => x.DeliveryFee).HasColumnName("delivery_fee").HasColumnType("decimal(14,2)").IsRequired();
            builder.Property(x => x.DeliveryRadiusKm).HasColumnName("delivery_radius_km").HasColumnType("decimal(5,2)").IsRequired();
            builder.Property(x => x.MaxActiveDeliveriesPerCourier)
                .HasColumnName("max_active_deliveries_per_courier")
                .HasDefaultValue(4)
                .IsRequired();

            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        }
    }
}
