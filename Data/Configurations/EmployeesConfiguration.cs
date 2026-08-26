using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Employees.Models;

namespace SaborExpress.Data.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("employees");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");

            builder.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(120)
                .IsRequired();
            builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(80);
            builder.Property(x => x.Document)
                .HasColumnName("document")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(x => x.Document).IsUnique();

            builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
            builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(255);
            builder.Property(x => x.Photo).HasColumnName("photo").HasMaxLength(255);

            builder.Property(x => x.BranchId).HasColumnName("branch_id");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(15)
                .HasDefaultValue("Activo");

            builder.Property(x => x.BasePay)
                .HasColumnName("base_pay")
                .HasColumnType("decimal(10,2)")
                .HasDefaultValue(0);



            builder.Property(x => x.CvFile)
                .HasColumnName("cv_file")
                 .HasColumnType("bytea");

            builder.Property(x => x.CvFilename)
                .HasColumnName("cv_filename")
                .HasMaxLength(255);

            builder.Property(x => x.CvContentType)
                .HasColumnName("cv_content_type")
                .HasMaxLength(100);

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Branch)
                .WithMany(b => b.Employees)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.UserId).IsUnique();
        }
    }
}