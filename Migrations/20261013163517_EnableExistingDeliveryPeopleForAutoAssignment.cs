using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class EnableExistingDeliveryPeopleForAutoAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Antes de existir esta preferencia, los repartidores activos ya podían
            // recibir pedidos. Inicializamos esos registros como disponibles para
            // conservar ese comportamiento al desplegar la asignación automática.
            migrationBuilder.Sql(
                """
                UPDATE employees e
                SET "IsAvailable" = TRUE
                WHERE e.status = 'Activo'
                  AND e."IsAvailable" = FALSE
                  AND EXISTS (
                      SELECT 1
                      FROM user_roles ur
                      INNER JOIN roles r ON r.id = ur.role_id
                      WHERE ur.user_id = e.user_id
                        AND r.name = 'Repartidor'
                  );
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAvailable",
                table: "employees",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsAvailable",
                table: "employees",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }
    }
}
