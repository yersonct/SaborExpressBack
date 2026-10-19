using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaborExpress.Data;

#nullable disable

namespace SaborExpress.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261018180000_AddCashClosures")]
public partial class AddCashClosures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "cash_closures",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                schedule_id = table.Column<int>(type: "integer", nullable: false),
                cashier_id = table.Column<int>(type: "integer", nullable: false),
                branch_id = table.Column<int>(type: "integer", nullable: false),
                shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                shift_started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                closed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                cash_expected = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                cash_counted = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                cash_difference = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                card_total = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                transfer_total = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                wompi_total = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                total_sales = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                payments_count = table.Column<int>(type: "integer", nullable: false),
                cashier_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                branch_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                payments_snapshot_json = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cash_closures", x => x.id);
                table.ForeignKey("FK_cash_closures_EmployeeSchedules_schedule_id", x => x.schedule_id, "EmployeeSchedules", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_cash_closures_employees_cashier_id", x => x.cashier_id, "employees", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_cash_closures_branches_branch_id", x => x.branch_id, "branches", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_cash_closures_schedule_id", table: "cash_closures", column: "schedule_id", unique: true);
        migrationBuilder.CreateIndex(name: "IX_cash_closures_cashier_id", table: "cash_closures", column: "cashier_id");
        migrationBuilder.CreateIndex(name: "IX_cash_closures_branch_id_shift_date", table: "cash_closures", columns: new[] { "branch_id", "shift_date" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "cash_closures");
}
