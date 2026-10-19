using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaborExpress.Data;

#nullable disable

namespace SaborExpress.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261018120000_AddCourierHeartbeat")]
    public partial class AddCourierHeartbeat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NULL para repartidores históricos: no se asignan hasta que abran la app.
            migrationBuilder.Sql("ALTER TABLE employees ADD COLUMN IF NOT EXISTS last_courier_heartbeat_at timestamp with time zone NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE employees DROP COLUMN IF EXISTS last_courier_heartbeat_at;");
        }
    }
}
