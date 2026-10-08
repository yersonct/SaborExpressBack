using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.UserPreferences.Models;

namespace SaborExpress.Data.Configurations
{
    public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
    {
        public void Configure(EntityTypeBuilder<UserSettings> builder)
        {
            builder.ToTable("user_settings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.UserId).IsUnique();

            builder.Property(x => x.Theme).HasColumnName("theme").HasMaxLength(10).IsRequired();
            builder.Property(x => x.EmailNotifications).HasColumnName("email_notifications").IsRequired();
            builder.Property(x => x.PushNotifications).HasColumnName("push_notifications").IsRequired();
            builder.Property(x => x.SoundNotifications).HasColumnName("sound_notifications").IsRequired();
            builder.Property(x => x.TimeZone).HasColumnName("time_zone").HasMaxLength(60).IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        }
    }
}