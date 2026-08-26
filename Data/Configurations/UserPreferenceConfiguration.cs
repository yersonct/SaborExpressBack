// Data/Configurations/UserPreferenceConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.UserPreferences.Models;

namespace SaborExpress.Data.Configurations
{
    public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
    {
        public void Configure(EntityTypeBuilder<UserPreference> builder)
        {
            builder.ToTable("user_preferences");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Screen)
                .HasColumnName("screen")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Key)
                .HasColumnName("key")
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(x => new { x.UserId, x.Screen, x.Key })
                .IsUnique();

            builder.Property(x => x.Value)
                .HasColumnName("value")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(x => x.DataType)
                .HasColumnName("data_type")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();
        }
    }
}