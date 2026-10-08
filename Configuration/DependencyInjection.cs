using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Modules.EmployeeSchedules.Repositories;
using SaborExpress.Modules.EmployeeSchedules.Services;
using SaborExpress.Modules.EmployeeSchedules.Validators;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Repositories;
using SaborExpress.Modules.Auth.Services;
using SaborExpress.Modules.Auth.Validators;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.Branches.Repositories;
using SaborExpress.Modules.Branches.Services;
using SaborExpress.Modules.Branches.Validators;
using SaborExpress.Modules.Customers.Interfaces;
using SaborExpress.Modules.Customers.Repositories;
using SaborExpress.Modules.Customers.Services;
using SaborExpress.Modules.Customers.Validators;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Employees.Repositories;
using SaborExpress.Modules.Employees.Services;
using SaborExpress.Modules.Employees.Validators;
using SaborExpress.Modules.Permissions.Interfaces;
using SaborExpress.Modules.Permissions.Repositories;
using SaborExpress.Modules.Permissions.Services;
using SaborExpress.Modules.Permissions.Validators;
using SaborExpress.Modules.RolePermissions.Interfaces;
using SaborExpress.Modules.RolePermissions.Repositories;
using SaborExpress.Modules.RolePermissions.Services;
using SaborExpress.Modules.RolePermissions.Validators;
using SaborExpress.Modules.Roles.Interfaces;
using SaborExpress.Modules.Roles.Repositories;
using SaborExpress.Modules.Roles.Services;
using SaborExpress.Modules.Roles.Validators;
using SaborExpress.Modules.UserRoles.Interfaces;
using SaborExpress.Modules.UserRoles.Repositories;
using SaborExpress.Modules.UserRoles.Services;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Shared.Services;
using SaborExpress.Modules.Categories.Interfaces;
using SaborExpress.Modules.Categories.Repositories;
using SaborExpress.Modules.Categories.Services;
using SaborExpress.Modules.Categories.Validators;
using SaborExpress.Modules.Products.Interfaces;
using SaborExpress.Modules.Products.Repositories;
using SaborExpress.Modules.Products.Services;
using SaborExpress.Modules.Products.Validators;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Repositories;
using SaborExpress.Modules.Orders.Services;
using SaborExpress.Modules.Orders.Validators;

using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Repositories;
using SaborExpress.Modules.Payments.Services;
using SaborExpress.Modules.Payments.Validators;

using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Modules.Tables.Repositories;
using SaborExpress.Modules.Tables.Services;
using SaborExpress.Modules.Tables.Validators;


using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Repositories;
using SaborExpress.Modules.Deliveries.Services;
using SaborExpress.Modules.Deliveries.Validators;

using SaborExpress.Modules.Reviews.Interfaces;
using SaborExpress.Modules.Reviews.Repositories;
using SaborExpress.Modules.Reviews.Services;
using SaborExpress.Modules.Reviews.Validators;

using SaborExpress.Modules.Addresses.Interfaces;
using SaborExpress.Modules.Addresses.Repositories;
using SaborExpress.Modules.Addresses.Services;
using SaborExpress.Modules.Addresses.Validators;

using SaborExpress.Modules.UserPreferences.Interfaces;
using SaborExpress.Modules.UserPreferences.Repositories;
using SaborExpress.Modules.UserPreferences.Services;
using SaborExpress.Modules.UserPreferences.Validators;

using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Modules.Configurations.Repositories;
using SaborExpress.Modules.Configurations.Services;
using SaborExpress.Modules.Configurations.Validators;



using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Notifications.Repositories;
using SaborExpress.Modules.Notifications.Services;
using SaborExpress.Shared.BackgroundServices;

using SaborExpress.Modules.DailyMenu.Interfaces;
using SaborExpress.Modules.DailyMenu.Repositories;
using SaborExpress.Modules.DailyMenu.Services;
using SaborExpress.Modules.DailyMenu.Validators;

using SaborExpress.Modules.Invoices.Interfaces;
using SaborExpress.Modules.Invoices.Repositories;
using SaborExpress.Modules.Invoices.Services;
using SaborExpress.Modules.Invoices.Validators;

namespace SaborExpress.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencies(this IServiceCollection services)
        {
            services.AddAuthModule();
            services.AddEmployeeSchedulesModule(); 
            services.AddSharedModule();
            services.AddRolesModule();
            services.AddPermissionsModule();
            services.AddRolePermissionsModule();
            services.AddUserRolesModule();
            services.AddCustomersModule();
            services.AddEmployeesModule();
            services.AddBranchesModule();
            services.AddCategoriesModule();
            services.AddProductsModule();
            services.AddOrderDetailModule();
            services.AddOrderDetailHistoryModule();
            services.AddOrderStatusHistoryModule();
            services.AddPaymentsModule();
            services.AddOrdersModule();
            services.AddTablesModule();
            services.AddDeliveriesModule();
            services.AddReviewsModule();
            services.AddAddressesModule();
            services.AddUserPreferencesModule();
            services.AddUserSettingsModule();
            services.AddConfigurationsModule();
            services.AddBranchOperationalSettingsModule();
            services.AddEmailConfirmationModule();
            services.AddNotificationsModule();
            services.AddBackgroundEmailQueueModule();
            services.AddDailyMenuModule();
            services.AddInvoicesModule();
            return services;
        }

        private static IServiceCollection AddAuthModule(this IServiceCollection services)
        {
            services.AddScoped<JwtTokenGenerator>();
            services.AddScoped<IAuthRepository, AuthRepository>();
             services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>(); 
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<LoginValidator>();

            services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<ForgotPasswordValidator>();
            services.AddScoped<VerifyResetCodeValidator>();
            services.AddScoped<ResetPasswordValidator>();

            services.AddScoped<IEmployeeActivationRepository, EmployeeActivationRepository>(); // ← nuevo
            services.AddScoped<IEmployeeActivationService, EmployeeActivationService>();       // ← nuevo
            services.AddScoped<ActivateAccountValidator>();     

            return services;
        }

        private static IServiceCollection AddSharedModule(this IServiceCollection services)
        {
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            return services;
        }

        private static IServiceCollection AddRolesModule(this IServiceCollection services)
        {
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<RoleValidator>();

            return services;
        }

        private static IServiceCollection AddPermissionsModule(this IServiceCollection services)
        {
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<PermissionValidator>();

            return services;
        }

        private static IServiceCollection AddRolePermissionsModule(this IServiceCollection services)
        {
            services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
            services.AddScoped<IRolePermissionService, RolePermissionService>();
            services.AddScoped<RolePermissionValidator>();

            return services;
        }

        private static IServiceCollection AddUserRolesModule(this IServiceCollection services)
        {
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IUserRoleService, UserRoleService>();

            return services;
        }

        private static IServiceCollection AddCustomersModule(this IServiceCollection services)
        {
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<CustomerValidator>();

            return services;
        }

        private static IServiceCollection AddEmployeesModule(this IServiceCollection services)
        {
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<EmployeeValidator>();

            return services;
        }

        private static IServiceCollection AddBranchesModule(this IServiceCollection services)
        {
            services.AddScoped<IBranchRepository, BranchRepository>();
            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<IBranchAccessGuard, BranchAccessGuard>(); 
            services.AddScoped<BranchValidator>();

            return services;
        }
        public static IServiceCollection AddCategoriesModule(this IServiceCollection services)
        {
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<CategoryValidator>();

            return services;
        }
        public static IServiceCollection AddProductsModule(this IServiceCollection services)
        {
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ProductValidator>();

            return services;
        }
        public static IServiceCollection AddOrderDetailModule(this IServiceCollection services)
        {
            services.AddScoped<IOrderDetailRepository, OrderDetailRepository>();
            services.AddScoped<IOrderDetailService, OrderDetailService>();
            services.AddScoped<OrderDetailValidator>();

            return services;
        }

        public static IServiceCollection AddOrderDetailHistoryModule(this IServiceCollection services)
        {
            services.AddScoped<IOrderDetailHistoryRepository, OrderDetailHistoryRepository>();
            services.AddScoped<IOrderDetailHistoryService, OrderDetailHistoryService>();
            // Sin Validator: esta entidad es de solo lectura, no tiene Create/Update manual

            return services;
        }
        public static IServiceCollection AddOrderStatusHistoryModule(this IServiceCollection services)
        {
            services.AddScoped<IOrderStatusHistoryRepository, OrderStatusHistoryRepository>();
            services.AddScoped<IOrderStatusHistoryService, OrderStatusHistoryService>();
            // Sin Validator: solo lectura

            return services;
        }

        public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
        {
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<PaymentValidator>();
            services.AddHttpClient<IWompiClient, WompiClient>();

            return services;
        }
        public static IServiceCollection AddTablesModule(this IServiceCollection services)
        {
            services.AddScoped<ITableRepository, TableRepository>();
            services.AddScoped<ITableService, TableService>();
            services.AddScoped<TableValidator>();

            return services;
        }

        public static IServiceCollection AddOrdersModule(this IServiceCollection services)
        {
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<OrderValidator>();

            return services;
        }

        public static IServiceCollection AddEmployeeSchedulesModule(this IServiceCollection services)
        {
            services.AddScoped<IEmployeeScheduleRepository, EmployeeScheduleRepository>();
            services.AddScoped<IEmployeeScheduleService, EmployeeScheduleService>();
            services.AddScoped<EmployeeScheduleValidator>();

            return services;
        }
        public static IServiceCollection AddDeliveriesModule(this IServiceCollection services)
        {
            services.AddScoped<IDeliveryRepository, DeliveryRepository>();
            services.AddScoped<IDeliveryService, DeliveryService>();
            services.AddScoped<IDeliveryAssignmentService, DeliveryAssignmentService>(); // NUEVO
            services.AddScoped<DeliveryValidator>();

            return services;
        }

        public static IServiceCollection AddReviewsModule(this IServiceCollection services)
        {
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<ReviewValidator>();

            return services;
        }

        public static IServiceCollection AddAddressesModule(this IServiceCollection services)
        {
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<AddressValidator>();

            return services;
        }

        public static IServiceCollection AddUserPreferencesModule(this IServiceCollection services)
        {
            services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
            services.AddScoped<IUserPreferenceService, UserPreferenceService>();
            services.AddScoped<UserPreferenceValidator>();

            return services;
        }

        public static IServiceCollection AddConfigurationsModule(this IServiceCollection services)
        {
            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
            services.AddScoped<IConfigurationService, ConfigurationService>();
            services.AddScoped<ConfigurationValidator>();

            return services;
        }
                public static IServiceCollection AddBranchOperationalSettingsModule(this IServiceCollection services)
        {
            services.AddScoped<IBranchOperationalSettingsRepository, BranchOperationalSettingsRepository>();
            services.AddScoped<IBranchOperationalSettingsService, BranchOperationalSettingsService>();
            services.AddScoped<BranchOperationalSettingsValidator>();

            return services;
        }

        public static IServiceCollection AddUserSettingsModule(this IServiceCollection services)
        {
            services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
            services.AddScoped<IUserSettingsService, UserSettingsService>();
            services.AddScoped<UserSettingsValidator>();

            return services;
        }
        public static IServiceCollection AddEmailConfirmationModule(this IServiceCollection services)
        {
            services.AddScoped<IEmailConfirmationRepository, EmailConfirmationRepository>();
            services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();

            return services;
        }
        public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
        {
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IUserNotificationService, UserNotificationService>();

            return services;
        }
        private static IServiceCollection AddBackgroundEmailQueueModule(this IServiceCollection services)
        {
            services.AddSingleton<IBackgroundEmailQueue, BackgroundEmailQueue>();
            services.AddHostedService<EmailQueueProcessor>();

            return services;
        }
        private static IServiceCollection AddDailyMenuModule(this IServiceCollection services)
        {
            services.AddScoped<IDailyMenuRepository, DailyMenuRepository>();
            services.AddScoped<IDailyMenuService, DailyMenuService>();
            services.AddScoped<DailyMenuValidator>();

            return services;
        }

        private static IServiceCollection AddInvoicesModule(this IServiceCollection services)
        {
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<InvoiceValidator>();

            return services;
        }

    }
}