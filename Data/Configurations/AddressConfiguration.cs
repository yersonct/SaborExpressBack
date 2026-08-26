// Data/Configurations/AddressConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Addresses.Models;

namespace SaborExpress.Data.Configurations
{
    public class AddressConfiguration : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.ToTable("addresses");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.CustomerId)
                .HasColumnName("customer_id")
                .IsRequired();

            builder.HasIndex(x => x.CustomerId);

            builder.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade); // si se borra el cliente, se borran sus direcciones

            builder.Property(x => x.AddressLine)
                .HasColumnName("address_line")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.Reference)
                .HasColumnName("reference")
                .HasMaxLength(255);

            builder.Property(x => x.Latitude)
                .HasColumnName("latitude")
                .HasColumnType("decimal(9,6)")
                .IsRequired();

            builder.Property(x => x.Longitude)
                .HasColumnName("longitude")
                .HasColumnType("decimal(9,6)")
                .IsRequired();

            builder.Property(x => x.IsDefault)
                .HasColumnName("is_default")
                .HasDefaultValue(false);
        }
    }
}