// Data/Configurations/EmployeeActivationCodeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Data.Configurations
{
    public class EmployeeActivationCodeConfiguration : IEntityTypeConfiguration<EmployeeActivationCode>
    {
        public void Configure(EntityTypeBuilder<EmployeeActivationCode> builder)
        {
            // Nombre de la tabla en la base de datos
            builder.ToTable("employee_activation_codes");

            // Llave primaria
            builder.HasKey(x => x.Id);

            // Mapeo de columnas
            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            // El código es de 6 dígitos, 20 es un buen límite de seguridad (igual que PasswordResetCode)
            builder.Property(x => x.Code)
                .HasColumnName("code")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.CodeExpiresAt)
                .HasColumnName("code_expires_at")
                .IsRequired();

            builder.Property(x => x.IsUsed)
                .HasColumnName("is_used")
                .HasDefaultValue(false);

            builder.Property(x => x.Attempts)
                .HasColumnName("attempts")
                .HasDefaultValue(0);

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            // Configuración de la Relación (Foreign Key) con la tabla Users
            builder.HasOne(x => x.User)
                .WithMany(u => u.EmployeeActivationCodes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra el usuario, se borran sus códigos
        }
    }
}