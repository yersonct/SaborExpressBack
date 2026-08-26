using Microsoft.EntityFrameworkCore;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Branches.Models;
using SaborExpress.Modules.Customers.Models;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Permissions.Models;
using SaborExpress.Modules.RolePermissions.Models;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Modules.Categories.Models;
using SaborExpress.Modules.Products.Models;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Payments.Models;
using SaborExpress.Modules.Tables.Models;
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Reviews.Models;
using SaborExpress.Modules.Addresses.Models;
using SaborExpress.Modules.UserPreferences.Models;
using SaborExpress.Modules.EmployeeSchedules.Models;
using SaborExpress.Modules.Configurations.Models;
using SaborExpress.Modules.DailyMenu.Models;
using SaborExpress.Modules.Invoices.Models;
using SaborExpress.Modules.Notifications.Models;
namespace SaborExpress.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {}
        public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Employee> Employees => Set<Employee>();

        public DbSet<Branch> Branches => Set<Branch>();

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<OrderDetailHistory> OrderDetailHistories => Set<OrderDetailHistory>();
        public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
        public DbSet<Payment>  Payments => Set<Payment>();

        public DbSet<Table> Tables => Set<Table>();

        public DbSet<Delivery> Deliveries => Set<Delivery>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<UserPreference> UserPreferences =>Set<UserPreference>();

        public DbSet<BranchSetting> Configurations  => Set<BranchSetting>();

        public DbSet<EmailConfirmationCode> EmailConfirmationCodes => Set<EmailConfirmationCode>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<EmployeeSchedule> EmployeeSchedules => Set<EmployeeSchedule>();
        public DbSet<DailyMenuItem> DailyMenuItems => Set<DailyMenuItem>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<EmployeeActivationCode> EmployeeActivationCodes => Set<EmployeeActivationCode>();
        public DbSet<Notification> Notifications  => Set<Notification>();
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aplica autom�ticamente todas las configuraciones
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }


}