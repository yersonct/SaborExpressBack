using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaborExpress.Modules.Tables.Models;

namespace SaborExpress.Data.Configurations
{
    public sealed class TableReleaseAuditConfiguration : IEntityTypeConfiguration<TableReleaseAudit>
    {
        public void Configure(EntityTypeBuilder<TableReleaseAudit> builder)
        {
            builder.ToTable("table_release_audits");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            builder.Property(x => x.OrderIdsJson).HasColumnType("text").IsRequired();
            builder.Property(x => x.OutstandingAmount).HasPrecision(18, 2);
            builder.HasIndex(x => new { x.BranchId, x.TableId, x.ReleasedAtUtc });
        }
    }
}
