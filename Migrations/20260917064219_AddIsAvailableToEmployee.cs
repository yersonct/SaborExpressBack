using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAvailableToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Longitude",
                table: "branches",
                newName: "longitude");

            migrationBuilder.RenameColumn(
                name: "Latitude",
                table: "branches",
                newName: "latitude");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "longitude",
                table: "branches",
                type: "numeric(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "latitude",
                table: "branches",
                type: "numeric(9,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "employees");

            migrationBuilder.RenameColumn(
                name: "longitude",
                table: "branches",
                newName: "Longitude");

            migrationBuilder.RenameColumn(
                name: "latitude",
                table: "branches",
                newName: "Latitude");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "branches",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "branches",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,6)");
        }
    }
}
