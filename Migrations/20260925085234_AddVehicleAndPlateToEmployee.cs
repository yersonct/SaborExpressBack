using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleAndPlateToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "photo",
                table: "employees");

            migrationBuilder.AddColumn<string>(
                name: "plate",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vehicle",
                table: "employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "plate",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "vehicle",
                table: "employees");

            migrationBuilder.AddColumn<string>(
                name: "photo",
                table: "employees",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "customers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
