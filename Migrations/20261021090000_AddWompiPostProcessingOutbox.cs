using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaborExpress.Data;

#nullable disable

namespace SaborExpress.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261021090000_AddWompiPostProcessingOutbox")]
    public sealed class AddWompiPostProcessingOutbox : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "wompi_post_processing_started_at_utc",
                table: "payments",
                type: null,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "wompi_post_processing_completed_at_utc",
                table: "payments",
                type: null,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Notifications",
                type: null,
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications",
                column: "IdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX_payments_wompi_post_processing",
                table: "payments",
                columns: new[]
                {
                    "method",
                    "status",
                    "wompi_post_processing_completed_at_utc",
                    "wompi_post_processing_started_at_utc"
                });

            // Los pagos ya aprobados antes de crear la cola no deben volver a
            // ejecutar notificaciones, liberar mesas o reasignar pedidos.
            migrationBuilder.Sql(
                "UPDATE payments SET wompi_post_processing_completed_at_utc = paid_at " +
                "WHERE method = 'Wompi' AND status = 'Completed'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_wompi_post_processing",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "wompi_post_processing_started_at_utc",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "wompi_post_processing_completed_at_utc",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Notifications");
        }
    }
}
