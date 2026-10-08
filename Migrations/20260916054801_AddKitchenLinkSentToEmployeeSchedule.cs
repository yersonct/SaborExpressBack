using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenLinkSentToEmployeeSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "KitchenLinkSent",
                table: "EmployeeSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KitchenLinkSent",
                table: "EmployeeSchedules");
        }
    }
}
