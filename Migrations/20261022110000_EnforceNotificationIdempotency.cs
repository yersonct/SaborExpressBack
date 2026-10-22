using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    public partial class EnforceNotificationIdempotency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the oldest durable notification for keys created by previous
            // non-atomic check-then-insert attempts.
            migrationBuilder.Sql(
                "DELETE FROM \"Notifications\" AS duplicate " +
                "USING \"Notifications\" AS keeper " +
                "WHERE duplicate.\"IdempotencyKey\" IS NOT NULL " +
                "AND duplicate.\"IdempotencyKey\" = keeper.\"IdempotencyKey\" " +
                "AND duplicate.\"Id\" > keeper.\"Id\"");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications",
                column: "IdempotencyKey");
        }
    }
}
