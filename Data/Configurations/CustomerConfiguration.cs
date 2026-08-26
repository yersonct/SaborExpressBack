using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Customers.Models;

namespace SaborExpress.Data.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("customers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(120)
                .IsRequired();

            builder.Property(x => x.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(120);

            builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);
            builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(255);
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasOne(x => x.User)
                .WithOne(u => u.Customer)
                .HasForeignKey<Customer>((x) => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId).IsUnique();
        }
    }
}