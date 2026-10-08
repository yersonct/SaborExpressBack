using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingToDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "customer_rating",
                table: "deliveries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_rating_comment",
                table: "deliveries",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rated_at",
                table: "deliveries",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "customer_rating",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "customer_rating_comment",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "rated_at",
                table: "deliveries");
        }
    }
}
