using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaborExpress.Data;

#nullable disable

namespace SaborExpress.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261011120000_AddPushDeviceNotificationProfile")]
    public partial class AddPushDeviceNotificationProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "notification_profile",
                table: "push_devices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "waiter");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notification_profile",
                table: "push_devices");
        }
    }
}
