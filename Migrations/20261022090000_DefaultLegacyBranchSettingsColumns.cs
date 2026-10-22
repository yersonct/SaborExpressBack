using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <summary>
    /// The original table contains legacy NOT NULL columns that are no longer
    /// mapped by BranchOperationalSettings. Defaults let current inserts omit them.
    /// </summary>
    public partial class DefaultLegacyBranchSettingsColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE branch_operational_settings " +
                "ALTER COLUMN suggested_tip_percent SET DEFAULT 0, " +
                "ALTER COLUMN min_order_amount SET DEFAULT 0, " +
                "ALTER COLUMN accepts_delivery SET DEFAULT TRUE, " +
                "ALTER COLUMN accepts_dine_in SET DEFAULT TRUE;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE branch_operational_settings " +
                "ALTER COLUMN suggested_tip_percent DROP DEFAULT, " +
                "ALTER COLUMN min_order_amount DROP DEFAULT, " +
                "ALTER COLUMN accepts_delivery DROP DEFAULT, " +
                "ALTER COLUMN accepts_dine_in DROP DEFAULT;");
        }
    }
}
