using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaborExpress.Data;

#nullable disable

namespace SaborExpress.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261018193000_AddCashClosureReopening")]
public partial class AddCashClosureReopening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(name: "reopened_at_utc", table: "cash_closures",
            type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<int>(name: "reopened_by_user_id", table: "cash_closures",
            type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "reopen_reason", table: "cash_closures",
            type: "character varying(500)", maxLength: 500, nullable: true);

        migrationBuilder.DropIndex(name: "IX_cash_closures_schedule_id", table: "cash_closures");
        migrationBuilder.CreateIndex(name: "IX_cash_closures_schedule_id", table: "cash_closures",
            column: "schedule_id", unique: true, filter: "reopened_at_utc IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Un rollback después de usar la reapertura destruiría la posibilidad
        // de conservar varios arqueos del mismo turno. Evitar pérdida silenciosa.
        migrationBuilder.DropIndex(name: "IX_cash_closures_schedule_id", table: "cash_closures");
        migrationBuilder.CreateIndex(name: "IX_cash_closures_schedule_id", table: "cash_closures",
            column: "schedule_id", unique: true);
        migrationBuilder.DropColumn(name: "reopened_at_utc", table: "cash_closures");
        migrationBuilder.DropColumn(name: "reopened_by_user_id", table: "cash_closures");
        migrationBuilder.DropColumn(name: "reopen_reason", table: "cash_closures");
    }
}
