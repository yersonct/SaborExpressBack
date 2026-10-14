using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SaborExpress.Data;

#nullable disable

namespace SaborExpress.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261012130000_RemoveDailyMenu")]
    public partial class RemoveDailyMenu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El menú del día deja de formar parte del catálogo y sus filas se eliminan.
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"DailyMenuItems\";");

            // Este permiso también autorizaba a cocina a activar/desactivar productos.
            // Renombrarlo conserva sus asignaciones sin mantener el permiso del menú.
            migrationBuilder.Sql("""
                DO $migration$
                DECLARE
                    old_permission_id integer;
                    product_permission_id integer;
                BEGIN
                    SELECT id INTO old_permission_id
                    FROM permissions
                    WHERE name = 'ACTIVAR_DESACTIVAR_PLATO';

                    SELECT id INTO product_permission_id
                    FROM permissions
                    WHERE name = 'ACTIVAR_DESACTIVAR_PRODUCTO';

                    IF old_permission_id IS NOT NULL AND product_permission_id IS NULL THEN
                        UPDATE permissions
                        SET name = 'ACTIVAR_DESACTIVAR_PRODUCTO',
                            module = 'Products',
                            description = 'Activar o desactivar productos'
                        WHERE id = old_permission_id;
                    ELSIF old_permission_id IS NOT NULL THEN
                        INSERT INTO role_permissions (role_id, permission_id, assigned_at)
                        SELECT role_id, product_permission_id, assigned_at
                        FROM role_permissions
                        WHERE permission_id = old_permission_id
                        ON CONFLICT (role_id, permission_id) DO NOTHING;

                        DELETE FROM role_permissions WHERE permission_id = old_permission_id;
                        DELETE FROM permissions WHERE id = old_permission_id;
                    END IF;

                    DELETE FROM role_permissions
                    WHERE permission_id IN (
                        SELECT id FROM permissions
                        WHERE name = 'GESTIONAR_MENU' OR module = 'DailyMenu'
                    );
                    DELETE FROM permissions
                    WHERE name = 'GESTIONAR_MENU' OR module = 'DailyMenu';
                END
                $migration$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyMenuItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MealPeriod = table.Column<int>(type: "integer", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyMenuItems_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyMenuItems_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyMenuItems_BranchId",
                table: "DailyMenuItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMenuItems_ProductId",
                table: "DailyMenuItems",
                column: "ProductId");

            migrationBuilder.Sql("""
                DO $migration$
                DECLARE
                    product_permission_id integer;
                    old_permission_id integer;
                BEGIN
                    SELECT id INTO product_permission_id
                    FROM permissions
                    WHERE name = 'ACTIVAR_DESACTIVAR_PRODUCTO';

                    SELECT id INTO old_permission_id
                    FROM permissions
                    WHERE name = 'ACTIVAR_DESACTIVAR_PLATO';

                    IF product_permission_id IS NOT NULL AND old_permission_id IS NULL THEN
                        UPDATE permissions
                        SET name = 'ACTIVAR_DESACTIVAR_PLATO',
                            module = 'DailyMenu',
                            description = 'Activar o desactivar un plato a media jornada'
                        WHERE id = product_permission_id;
                    ELSIF product_permission_id IS NOT NULL THEN
                        INSERT INTO role_permissions (role_id, permission_id, assigned_at)
                        SELECT role_id, old_permission_id, assigned_at
                        FROM role_permissions
                        WHERE permission_id = product_permission_id
                        ON CONFLICT (role_id, permission_id) DO NOTHING;

                        DELETE FROM role_permissions WHERE permission_id = product_permission_id;
                        DELETE FROM permissions WHERE id = product_permission_id;
                    END IF;

                    INSERT INTO permissions (name, module, description, status, created_at)
                    VALUES (
                        'GESTIONAR_MENU', 'DailyMenu', 'Planear el menú del día', true, CURRENT_TIMESTAMP
                    )
                    ON CONFLICT (name) DO NOTHING;

                    INSERT INTO role_permissions (role_id, permission_id, assigned_at)
                    SELECT roles.id, permissions.id, CURRENT_TIMESTAMP
                    FROM roles
                    CROSS JOIN permissions
                    WHERE roles.name IN ('GERENTE', 'ADMINISTRADOR')
                      AND permissions.name = 'GESTIONAR_MENU'
                    ON CONFLICT (role_id, permission_id) DO NOTHING;
                END
                $migration$;
                """);
        }
    }
}
