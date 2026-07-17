using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class SaborExpress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    document = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
