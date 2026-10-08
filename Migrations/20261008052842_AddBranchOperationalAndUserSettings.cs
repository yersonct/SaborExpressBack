using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchOperationalAndUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branch_operational_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    branch_id = table.Column<int>(type: "integer", nullable: false),
                    opening_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    closing_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    delivery_fee = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    suggested_tip_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    delivery_radius_km = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    min_order_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    accepts_delivery = table.Column<bool>(type: "boolean", nullable: false),
                    accepts_dine_in = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_operational_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_operational_settings_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    theme = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    email_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    push_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    sound_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    time_zone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_operational_settings_branch_id",
                table: "branch_operational_settings",
                column: "branch_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_user_id",
                table: "user_settings",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_operational_settings");

            migrationBuilder.DropTable(
                name: "user_settings");
        }
    }
}
