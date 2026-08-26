using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;

namespace SaborExpress.Configuration
{
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var dbProvider = configuration.GetValue<string>("DatabaseProvider");

            services.AddDbContext<AppDbContext>(options =>
                ConfigurarProveedor(options, dbProvider, configuration));

            return services;
        }

        private static void ConfigurarProveedor(
            DbContextOptionsBuilder options, string? dbProvider, IConfiguration configuration)
        {
            switch (dbProvider)
            {
                case "MySQL":
                    ConfigurarMySql(options, configuration);
                    break;

                case "SqlServer":
                    ConfigurarSqlServer(options, configuration);
                    break;

                case "PostgreSQL":
                    ConfigurarPostgres(options, configuration);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Proveedor de base de datos no soportado: {dbProvider}");
            }
        }

        private static void ConfigurarMySql(DbContextOptionsBuilder options, IConfiguration configuration)
        {
            var connectionString = ObtenerConnectionString(configuration, "MySQLConnection");
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }

        private static void ConfigurarSqlServer(DbContextOptionsBuilder options, IConfiguration configuration)
        {
            var connectionString = ObtenerConnectionString(configuration, "SqlServerConnection");
            options.UseSqlServer(connectionString);
        }

        private static void ConfigurarPostgres(DbContextOptionsBuilder options, IConfiguration configuration)
        {
            var connectionString = ObtenerConnectionString(configuration, "PostgresConnection");
            options.UseNpgsql(connectionString);
        }

        private static string ObtenerConnectionString(IConfiguration configuration, string key)
        {
            var connectionString = configuration.GetConnectionString(key);

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception($"{key} no está configurada.");

            return connectionString;
        }
    }
}