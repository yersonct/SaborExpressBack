using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaborExpress.Data;

#nullable disable

namespace SaborExpress.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261012140000_AddLocalizedMenuContent")]
    public partial class AddLocalizedMenuContent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name_en", table: "categories", type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "name_pt", table: "categories", type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "name_fr", table: "categories", type: "character varying(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "description_en", table: "categories", type: "character varying(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "description_pt", table: "categories", type: "character varying(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "description_fr", table: "categories", type: "character varying(500)", maxLength: 500, nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name_en", table: "products", type: "character varying(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "name_pt", table: "products", type: "character varying(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "name_fr", table: "products", type: "character varying(120)", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "description_en", table: "products", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "description_pt", table: "products", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "description_fr", table: "products", type: "text", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "name_en", table: "categories");
            migrationBuilder.DropColumn(name: "name_pt", table: "categories");
            migrationBuilder.DropColumn(name: "name_fr", table: "categories");
            migrationBuilder.DropColumn(name: "description_en", table: "categories");
            migrationBuilder.DropColumn(name: "description_pt", table: "categories");
            migrationBuilder.DropColumn(name: "description_fr", table: "categories");

            migrationBuilder.DropColumn(name: "name_en", table: "products");
            migrationBuilder.DropColumn(name: "name_pt", table: "products");
            migrationBuilder.DropColumn(name: "name_fr", table: "products");
            migrationBuilder.DropColumn(name: "description_en", table: "products");
            migrationBuilder.DropColumn(name: "description_pt", table: "products");
            migrationBuilder.DropColumn(name: "description_fr", table: "products");
        }
    }
}
