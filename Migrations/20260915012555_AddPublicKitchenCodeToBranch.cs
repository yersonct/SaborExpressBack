using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicKitchenCodeToBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "public_kitchen_code",
                table: "branches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_branches_public_kitchen_code",
                table: "branches",
                column: "public_kitchen_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_branches_public_kitchen_code",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "public_kitchen_code",
                table: "branches");
        }
    }
}
