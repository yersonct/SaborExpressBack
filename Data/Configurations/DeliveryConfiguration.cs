// Data/Configurations/DeliveryConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Deliveries.Models;

namespace SaborExpress.Data.Configurations
{
    public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
    {
        public void Configure(EntityTypeBuilder<Delivery> builder)
        {
            builder.ToTable("deliveries");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.OrderId)
                .HasColumnName("order_id")
                .IsRequired();

            builder.HasIndex(x => x.OrderId)
                .IsUnique(); // un pedido solo puede tener un domicilio asignado

            builder.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.AddressId)
                .HasColumnName("address_id")
                .IsRequired();

            builder.HasOne(x => x.Address)
                .WithMany()
                .HasForeignKey(x => x.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.DeliveryPersonId)
                .HasColumnName("delivery_person_id")
                .IsRequired();

            builder.HasIndex(x => x.DeliveryPersonId);

            builder.HasOne(x => x.DeliveryPerson)
                .WithMany()
                .HasForeignKey(x => x.DeliveryPersonId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.AssignedAt)
                .HasColumnName("assigned_at")
                .IsRequired();

            builder.Property(x => x.DeliveredAt)
                .HasColumnName("delivered_at");
        }
    }
}