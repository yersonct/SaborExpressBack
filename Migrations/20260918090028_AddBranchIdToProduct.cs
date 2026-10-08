using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_BranchId",
                table: "products",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_products_branches_BranchId",
                table: "products",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_branches_BranchId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_BranchId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "products");
        }
    }
}
