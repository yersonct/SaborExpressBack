// Data/Configurations/BranchSettingConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Configurations.Models;

namespace SaborExpress.Data.Configurations
{
    public class BranchSettingConfiguration : IEntityTypeConfiguration<BranchSetting>
    {
        public void Configure(EntityTypeBuilder<BranchSetting> builder)
        {
            builder.ToTable("branch_settings");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.BranchId).HasColumnName("branch_id");

            builder.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Key)
                .HasColumnName("key")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Value)
                .HasColumnName("value")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(x => x.DataType)
                .HasColumnName("data_type")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.HasIndex(x => new { x.BranchId, x.Key })
                .IsUnique()
                .HasFilter("branch_id IS NOT NULL");

            builder.HasIndex(x => x.Key)
                .IsUnique()
                .HasFilter("branch_id IS NULL");
        }
    }
}