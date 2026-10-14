using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierLocationAndCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "current_latitude",
                table: "employees",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "current_longitude",
                table: "employees",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "location_updated_at",
                table: "employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_active_deliveries_per_courier",
                table: "branch_operational_settings",
                type: "integer",
                nullable: false,
                defaultValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_latitude",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "current_longitude",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "location_updated_at",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "max_active_deliveries_per_courier",
                table: "branch_operational_settings");
        }
    }
}
