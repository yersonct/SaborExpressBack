using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Notifications.Models;

namespace SaborExpress.Data.Configurations
{
    public class PushDeviceConfiguration : IEntityTypeConfiguration<PushDevice>
    {
        public void Configure(EntityTypeBuilder<PushDevice> builder)
        {
            builder.ToTable("push_devices");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(x => x.ExpoPushToken)
                .HasColumnName("expo_push_token")
                .HasMaxLength(256)
                .IsRequired();
            builder.Property(x => x.Platform)
                .HasColumnName("platform")
                .HasMaxLength(20)
                .IsRequired();
            builder.Property(x => x.NotificationProfile)
                .HasColumnName("notification_profile")
                .HasMaxLength(20)
                .HasDefaultValue("waiter")
                .IsRequired();
            builder.Property(x => x.SoundEnabled).HasColumnName("sound_enabled").IsRequired();
            builder.Property(x => x.VibrationEnabled).HasColumnName("vibration_enabled").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ExpoPushToken).IsUnique();
            builder.HasIndex(x => x.UserId);
        }
    }
}
