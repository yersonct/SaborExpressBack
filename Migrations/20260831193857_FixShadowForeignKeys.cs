using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class FixShadowForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_details_orders_OrderId1",
                table: "order_details");

            migrationBuilder.DropForeignKey(
                name: "FK_order_status_history_orders_OrderId1",
                table: "order_status_history");

            migrationBuilder.DropForeignKey(
                name: "FK_password_reset_codes_users_UserId1",
                table: "password_reset_codes");

            migrationBuilder.DropForeignKey(
                name: "FK_products_categories_CategoryId1",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_CategoryId1",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_password_reset_codes_UserId1",
                table: "password_reset_codes");

            migrationBuilder.DropIndex(
                name: "IX_order_status_history_OrderId1",
                table: "order_status_history");

            migrationBuilder.DropIndex(
                name: "IX_order_details_OrderId1",
                table: "order_details");

            migrationBuilder.DropColumn(
                name: "CategoryId1",
                table: "products");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "password_reset_codes");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "order_status_history");

            migrationBuilder.DropColumn(
                name: "OrderId1",
                table: "order_details");

            migrationBuilder.AlterColumn<int>(
                name: "cashier_id",
                table: "payments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "wompi_reference",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "wompi_transaction_id",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "employee_id",
                table: "orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "changed_by_employee_id",
                table: "order_status_history",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_payments_wompi_reference",
                table: "payments",
                column: "wompi_reference",
                unique: true,
                filter: "wompi_reference IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_wompi_reference",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "wompi_reference",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "wompi_transaction_id",
                table: "payments");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId1",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "cashier_id",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "password_reset_codes",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "employee_id",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "changed_by_employee_id",
                table: "order_status_history",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "order_status_history",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderId1",
                table: "order_details",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_CategoryId1",
                table: "products",
                column: "CategoryId1");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_codes_UserId1",
                table: "password_reset_codes",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_order_status_history_OrderId1",
                table: "order_status_history",
                column: "OrderId1");

            migrationBuilder.CreateIndex(
                name: "IX_order_details_OrderId1",
                table: "order_details",
                column: "OrderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_order_details_orders_OrderId1",
                table: "order_details",
                column: "OrderId1",
                principalTable: "orders",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_status_history_orders_OrderId1",
                table: "order_status_history",
                column: "OrderId1",
                principalTable: "orders",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_password_reset_codes_users_UserId1",
                table: "password_reset_codes",
                column: "UserId1",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_products_categories_CategoryId1",
                table: "products",
                column: "CategoryId1",
                principalTable: "categories",
                principalColumn: "id");
        }
    }
}
