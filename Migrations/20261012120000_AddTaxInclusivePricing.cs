using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaborExpress.Data;

#nullable disable

namespace SaborExpress.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261012120000_AddTaxInclusivePricing")]
    public partial class AddTaxInclusivePricing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Algunas bases ya tienen columnas creadas fuera del historial de EF.
            // IF NOT EXISTS permite completar la migración sin borrar ni recrear esos datos.
            migrationBuilder.Sql("ALTER TABLE orders ADD COLUMN IF NOT EXISTS tax_rate decimal(5,2) NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE orders ADD COLUMN IF NOT EXISTS delivery_fee decimal(12,2) NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE orders ADD COLUMN IF NOT EXISTS tax_included_in_price boolean NOT NULL DEFAULT false;");

            migrationBuilder.Sql("ALTER TABLE \"Invoices\" ADD COLUMN IF NOT EXISTS tax_rate decimal(5,2) NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE \"Invoices\" ADD COLUMN IF NOT EXISTS delivery_fee decimal(12,2) NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE \"Invoices\" ADD COLUMN IF NOT EXISTS tax_included_in_price boolean NOT NULL DEFAULT false;");

            // Recupera el porcentaje y el cobro de domicilio de pedidos antiguos
            // sin recalcular sus totales guardados.
            migrationBuilder.Sql("""
                UPDATE orders
                SET tax_rate = CASE
                    WHEN sub_total > 0 THEN ROUND((tax / sub_total) * 100, 2)
                    ELSE 0
                END,
                delivery_fee = CASE
                    WHEN order_type = 'Delivery' THEN GREATEST(total - sub_total - tax, 0)
                    ELSE 0
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE "Invoices" AS invoice
                SET tax_rate = orders.tax_rate,
                    delivery_fee = orders.delivery_fee
                FROM orders
                WHERE invoice."OrderId" = orders.id;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "tax_included_in_price", table: "Invoices");
            migrationBuilder.DropColumn(name: "delivery_fee", table: "Invoices");
            migrationBuilder.DropColumn(name: "tax_rate", table: "Invoices");

            migrationBuilder.DropColumn(
                name: "tax_included_in_price",
                table: "orders");

            migrationBuilder.DropColumn(name: "delivery_fee", table: "orders");
            migrationBuilder.DropColumn(name: "tax_rate", table: "orders");
        }
    }
}
