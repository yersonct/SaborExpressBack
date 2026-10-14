using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundReasonToPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "refund_reason",
                table: "payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "refund_reason",
                table: "payments");
        }
    }
}
