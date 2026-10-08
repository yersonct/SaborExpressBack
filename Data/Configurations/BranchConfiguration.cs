using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Branches.Models;

namespace SaborExpress.Data.Configurations
{
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.ToTable("branches");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            builder.HasIndex(x => x.Name).IsUnique();

            builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(255);
            builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);
            builder.Property(x => x.Status).HasColumnName("status").HasDefaultValue(true);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.Property(x => x.Latitude)
                .HasColumnName("latitude")
                .HasColumnType("decimal(9,6)")
                .IsRequired();

            builder.Property(x => x.Longitude)
                .HasColumnName("longitude")
                .HasColumnType("decimal(9,6)")
                .IsRequired();

            builder.Property(x => x.PublicKitchenCode)
                .HasColumnName("public_kitchen_code")
                .HasMaxLength(20);
            builder.HasIndex(x => x.PublicKitchenCode).IsUnique();
        }
    }
}