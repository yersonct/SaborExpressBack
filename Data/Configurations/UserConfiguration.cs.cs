using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(120);

            builder.Property(x => x.Document)
                .HasColumnName("document")
                .HasMaxLength(20);

            builder.Property(x => x.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired();

            builder.Property(x => x.RoleId)
                .HasColumnName("role_id")
                .IsRequired();

            builder.Property(x => x.Token)
                .HasColumnName("token");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasDefaultValue(true);

            builder.Property(x => x.LastLogin)
                .HasColumnName("last_login");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at");
        }
    }
}