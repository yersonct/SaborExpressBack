using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaborExpress.Migrations
{
    /// <inheritdoc />
    public partial class AuditCorrections20261030 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cash_closures_EmployeeSchedules_schedule_id",
                table: "cash_closures");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailConfirmationCodes_users_UserId",
                table: "EmailConfirmationCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeSchedules_branches_BranchId",
                table: "EmployeeSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeSchedules_employees_EmployeeId",
                table: "EmployeeSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeSchedules_roles_RoleId",
                table: "EmployeeSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_branches_BranchId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_orders_OrderId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_payments_PaymentId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_users_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_users_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Invoices",
                table: "Invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmployeeSchedules",
                table: "EmployeeSchedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailConfirmationCodes",
                table: "EmailConfirmationCodes");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "notifications");

            migrationBuilder.RenameTable(
                name: "Invoices",
                newName: "invoices");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "refresh_tokens");

            migrationBuilder.RenameTable(
                name: "EmployeeSchedules",
                newName: "employee_schedules");

            migrationBuilder.RenameTable(
                name: "EmailConfirmationCodes",
                newName: "email_confirmation_codes");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "notifications",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "notifications",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "notifications",
                newName: "message");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "notifications",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "notifications",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "RelatedEntityType",
                table: "notifications",
                newName: "related_entity_type");

            migrationBuilder.RenameColumn(
                name: "RelatedEntityId",
                table: "notifications",
                newName: "related_entity_id");

            migrationBuilder.RenameColumn(
                name: "ReadAt",
                table: "notifications",
                newName: "read_at");

            migrationBuilder.RenameColumn(
                name: "IsRead",
                table: "notifications",
                newName: "is_read");

            migrationBuilder.RenameColumn(
                name: "IdempotencyKey",
                table: "notifications",
                newName: "idempotency_key");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "notifications",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "notifications",
                newName: "IX_notifications_user_id_is_read");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "invoices",
                newName: "total");

            migrationBuilder.RenameColumn(
                name: "Tax",
                table: "invoices",
                newName: "tax");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "invoices",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VoidReason",
                table: "invoices",
                newName: "void_reason");

            migrationBuilder.RenameColumn(
                name: "SubTotal",
                table: "invoices",
                newName: "sub_total");

            migrationBuilder.RenameColumn(
                name: "PaymentId",
                table: "invoices",
                newName: "payment_id");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "invoices",
                newName: "order_id");

            migrationBuilder.RenameColumn(
                name: "IssuedAt",
                table: "invoices",
                newName: "issued_at");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "invoices",
                newName: "is_voided");

            migrationBuilder.RenameColumn(
                name: "InvoiceNumber",
                table: "invoices",
                newName: "invoice_number");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "invoices",
                newName: "branch_id");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_PaymentId",
                table: "invoices",
                newName: "IX_invoices_payment_id");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_OrderId",
                table: "invoices",
                newName: "IX_invoices_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "invoices",
                newName: "IX_invoices_invoice_number");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_BranchId",
                table: "invoices",
                newName: "IX_invoices_branch_id");

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "refresh_tokens",
                newName: "token");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "refresh_tokens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "refresh_tokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "IsRevoked",
                table: "refresh_tokens",
                newName: "is_revoked");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "refresh_tokens",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "refresh_tokens",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_user_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "employee_schedules",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "employee_schedules",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "employee_schedules",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "employee_schedules",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "employee_schedules",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "ShiftDate",
                table: "employee_schedules",
                newName: "shift_date");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "employee_schedules",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "ReminderSent",
                table: "employee_schedules",
                newName: "reminder_sent");

            migrationBuilder.RenameColumn(
                name: "KitchenLinkSent",
                table: "employee_schedules",
                newName: "kitchen_link_sent");

            migrationBuilder.RenameColumn(
                name: "KitchenAccessToken",
                table: "employee_schedules",
                newName: "kitchen_access_token");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "employee_schedules",
                newName: "end_time");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "employee_schedules",
                newName: "employee_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "employee_schedules",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BranchId",
                table: "employee_schedules",
                newName: "branch_id");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeeSchedules_RoleId",
                table: "employee_schedules",
                newName: "IX_employee_schedules_role_id");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeeSchedules_EmployeeId",
                table: "employee_schedules",
                newName: "IX_employee_schedules_employee_id");

            migrationBuilder.RenameIndex(
                name: "IX_EmployeeSchedules_BranchId",
                table: "employee_schedules",
                newName: "IX_employee_schedules_branch_id");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "email_confirmation_codes",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "Attempts",
                table: "email_confirmation_codes",
                newName: "attempts");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "email_confirmation_codes",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "email_confirmation_codes",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "IsUsed",
                table: "email_confirmation_codes",
                newName: "is_used");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "email_confirmation_codes",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CodeExpiresAt",
                table: "email_confirmation_codes",
                newName: "code_expires_at");

            migrationBuilder.RenameIndex(
                name: "IX_EmailConfirmationCodes_UserId",
                table: "email_confirmation_codes",
                newName: "IX_email_confirmation_codes_user_id");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "products",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "payments",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "total",
                table: "orders",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "tax",
                table: "orders",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "sub_total",
                table: "orders",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "delivery_fee",
                table: "orders",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "order_details",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "sub_total",
                table: "order_details",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "delivery_fee",
                table: "invoices",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "base_pay",
                table: "employees",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "privacy_accepted_at_utc",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "privacy_policy_version",
                table: "customers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "delivery_fee",
                table: "branch_operational_settings",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notifications",
                table: "notifications",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_invoices",
                table: "invoices",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_employee_schedules",
                table: "employee_schedules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_email_confirmation_codes",
                table: "email_confirmation_codes",
                column: "id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reviews_rating",
                table: "reviews",
                sql: "rating BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_price",
                table: "products",
                sql: "price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payments_amount",
                table: "payments",
                sql: "amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_money",
                table: "orders",
                sql: "sub_total >= 0 AND tax >= 0 AND delivery_fee >= 0 AND total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_details_money",
                table: "order_details",
                sql: "unit_price >= 0 AND sub_total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_details_quantity",
                table: "order_details",
                sql: "quantity > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "notifications",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_deliveries_rating",
                table: "deliveries",
                sql: "customer_rating IS NULL OR customer_rating BETWEEN 1 AND 5");

            migrationBuilder.AddForeignKey(
                name: "FK_cash_closures_employee_schedules_schedule_id",
                table: "cash_closures",
                column: "schedule_id",
                principalTable: "employee_schedules",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_email_confirmation_codes_users_user_id",
                table: "email_confirmation_codes",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_schedules_branches_branch_id",
                table: "employee_schedules",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_schedules_employees_employee_id",
                table: "employee_schedules",
                column: "employee_id",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_employee_schedules_roles_role_id",
                table: "employee_schedules",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_branches_branch_id",
                table: "invoices",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_orders_order_id",
                table: "invoices",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_payments_payment_id",
                table: "invoices",
                column: "payment_id",
                principalTable: "payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_users_user_id",
                table: "notifications",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_users_user_id",
                table: "refresh_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cash_closures_employee_schedules_schedule_id",
                table: "cash_closures");

            migrationBuilder.DropForeignKey(
                name: "FK_email_confirmation_codes_users_user_id",
                table: "email_confirmation_codes");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_schedules_branches_branch_id",
                table: "employee_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_schedules_employees_employee_id",
                table: "employee_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_employee_schedules_roles_role_id",
                table: "employee_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_invoices_branches_branch_id",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_invoices_orders_order_id",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_invoices_payments_payment_id",
                table: "invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_users_user_id",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_user_id",
                table: "refresh_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reviews_rating",
                table: "reviews");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_price",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payments_amount",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_money",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_details_money",
                table: "order_details");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_details_quantity",
                table: "order_details");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notifications",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_invoices",
                table: "invoices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_deliveries_rating",
                table: "deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_employee_schedules",
                table: "employee_schedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_email_confirmation_codes",
                table: "email_confirmation_codes");

            migrationBuilder.DropColumn(
                name: "privacy_accepted_at_utc",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "privacy_policy_version",
                table: "customers");

            migrationBuilder.RenameTable(
                name: "notifications",
                newName: "Notifications");

            migrationBuilder.RenameTable(
                name: "invoices",
                newName: "Invoices");

            migrationBuilder.RenameTable(
                name: "refresh_tokens",
                newName: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "employee_schedules",
                newName: "EmployeeSchedules");

            migrationBuilder.RenameTable(
                name: "email_confirmation_codes",
                newName: "EmailConfirmationCodes");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Notifications",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Notifications",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "message",
                table: "Notifications",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Notifications",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Notifications",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "related_entity_type",
                table: "Notifications",
                newName: "RelatedEntityType");

            migrationBuilder.RenameColumn(
                name: "related_entity_id",
                table: "Notifications",
                newName: "RelatedEntityId");

            migrationBuilder.RenameColumn(
                name: "read_at",
                table: "Notifications",
                newName: "ReadAt");

            migrationBuilder.RenameColumn(
                name: "is_read",
                table: "Notifications",
                newName: "IsRead");

            migrationBuilder.RenameColumn(
                name: "idempotency_key",
                table: "Notifications",
                newName: "IdempotencyKey");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Notifications",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_notifications_user_id_is_read",
                table: "Notifications",
                newName: "IX_Notifications_UserId_IsRead");

            migrationBuilder.RenameColumn(
                name: "total",
                table: "Invoices",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "tax",
                table: "Invoices",
                newName: "Tax");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Invoices",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "void_reason",
                table: "Invoices",
                newName: "VoidReason");

            migrationBuilder.RenameColumn(
                name: "sub_total",
                table: "Invoices",
                newName: "SubTotal");

            migrationBuilder.RenameColumn(
                name: "payment_id",
                table: "Invoices",
                newName: "PaymentId");

            migrationBuilder.RenameColumn(
                name: "order_id",
                table: "Invoices",
                newName: "OrderId");

            migrationBuilder.RenameColumn(
                name: "issued_at",
                table: "Invoices",
                newName: "IssuedAt");

            migrationBuilder.RenameColumn(
                name: "is_voided",
                table: "Invoices",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "invoice_number",
                table: "Invoices",
                newName: "InvoiceNumber");

            migrationBuilder.RenameColumn(
                name: "branch_id",
                table: "Invoices",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_invoices_payment_id",
                table: "Invoices",
                newName: "IX_Invoices_PaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_invoices_order_id",
                table: "Invoices",
                newName: "IX_Invoices_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_invoices_invoice_number",
                table: "Invoices",
                newName: "IX_Invoices_InvoiceNumber");

            migrationBuilder.RenameIndex(
                name: "IX_invoices_branch_id",
                table: "Invoices",
                newName: "IX_Invoices_BranchId");

            migrationBuilder.RenameColumn(
                name: "token",
                table: "RefreshTokens",
                newName: "Token");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "RefreshTokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "RefreshTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "is_revoked",
                table: "RefreshTokens",
                newName: "IsRevoked");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "RefreshTokens",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "RefreshTokens",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_user_id",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_UserId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "EmployeeSchedules",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "EmployeeSchedules",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "EmployeeSchedules",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "EmployeeSchedules",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "EmployeeSchedules",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "shift_date",
                table: "EmployeeSchedules",
                newName: "ShiftDate");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "EmployeeSchedules",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "reminder_sent",
                table: "EmployeeSchedules",
                newName: "ReminderSent");

            migrationBuilder.RenameColumn(
                name: "kitchen_link_sent",
                table: "EmployeeSchedules",
                newName: "KitchenLinkSent");

            migrationBuilder.RenameColumn(
                name: "kitchen_access_token",
                table: "EmployeeSchedules",
                newName: "KitchenAccessToken");

            migrationBuilder.RenameColumn(
                name: "end_time",
                table: "EmployeeSchedules",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "employee_id",
                table: "EmployeeSchedules",
                newName: "EmployeeId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "EmployeeSchedules",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "branch_id",
                table: "EmployeeSchedules",
                newName: "BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_employee_schedules_role_id",
                table: "EmployeeSchedules",
                newName: "IX_EmployeeSchedules_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_employee_schedules_employee_id",
                table: "EmployeeSchedules",
                newName: "IX_EmployeeSchedules_EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_employee_schedules_branch_id",
                table: "EmployeeSchedules",
                newName: "IX_EmployeeSchedules_BranchId");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "EmailConfirmationCodes",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "attempts",
                table: "EmailConfirmationCodes",
                newName: "Attempts");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "EmailConfirmationCodes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "EmailConfirmationCodes",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "is_used",
                table: "EmailConfirmationCodes",
                newName: "IsUsed");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "EmailConfirmationCodes",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "code_expires_at",
                table: "EmailConfirmationCodes",
                newName: "CodeExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_email_confirmation_codes_user_id",
                table: "EmailConfirmationCodes",
                newName: "IX_EmailConfirmationCodes_UserId");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "products",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "payments",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "total",
                table: "orders",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "tax",
                table: "orders",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "sub_total",
                table: "orders",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "delivery_fee",
                table: "orders",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "order_details",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "sub_total",
                table: "order_details",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "delivery_fee",
                table: "Invoices",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "base_pay",
                table: "employees",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "delivery_fee",
                table: "branch_operational_settings",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Invoices",
                table: "Invoices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmployeeSchedules",
                table: "EmployeeSchedules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailConfirmationCodes",
                table: "EmailConfirmationCodes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_cash_closures_EmployeeSchedules_schedule_id",
                table: "cash_closures",
                column: "schedule_id",
                principalTable: "EmployeeSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmailConfirmationCodes_users_UserId",
                table: "EmailConfirmationCodes",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeSchedules_branches_BranchId",
                table: "EmployeeSchedules",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeSchedules_employees_EmployeeId",
                table: "EmployeeSchedules",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeSchedules_roles_RoleId",
                table: "EmployeeSchedules",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_branches_BranchId",
                table: "Invoices",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_orders_OrderId",
                table: "Invoices",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_payments_PaymentId",
                table: "Invoices",
                column: "PaymentId",
                principalTable: "payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_users_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_users_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
