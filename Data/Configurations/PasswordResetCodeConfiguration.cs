using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Data.Configurations
{
    public class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode>
    {
        public void Configure(EntityTypeBuilder<PasswordResetCode> builder)
        {
            // Nombre de la tabla en la base de datos
            builder.ToTable("password_reset_codes");

            // Llave primaria
            builder.HasKey(x => x.Id);

            // Mapeo de columnas
            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            // El c�digo suele ser corto (ej. 6 d�gitos), 20 es un buen l�mite de seguridad
            builder.Property(x => x.Code)
                .HasColumnName("code")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.CodeExpiresAt)
                .HasColumnName("code_expires_at")
                .IsRequired();

            builder.Property(x => x.IsUsed)
                .HasColumnName("is_code_used")
                .HasDefaultValue(false);

            // El token suele ser un string largo generado (como un Guid o JWT)
            builder.Property(x => x.ResetToken)
                .HasColumnName("reset_token")
                .HasMaxLength(255);

            builder.Property(x => x.ResetTokenExpiresAt)
                .HasColumnName("reset_token_expires_at");

            builder.Property(x => x.IsCompleted)
                .HasColumnName("is_completed")
                .HasDefaultValue(false);

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            // Configuraci�n de la Relaci�n (Foreign Key) con la tabla Users
            builder.HasOne(x => x.User)
                .WithMany() // Dejar vac�o si User no tiene una lista de c�digos
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra el usuario, se borran sus c�digos
        }
    }
}