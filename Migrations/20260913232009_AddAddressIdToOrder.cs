using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressIdToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AddressId",
                table: "orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_AddressId",
                table: "orders",
                column: "AddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_addresses_AddressId",
                table: "orders",
                column: "AddressId",
                principalTable: "addresses",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_addresses_AddressId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_AddressId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "orders");
        }
    }
}
