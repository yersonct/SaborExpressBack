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
            {
                switch (dbProvider)
                {
                    case "MySQL":
                        {
                            var mySql = configuration.GetConnectionString("MySQLConnection");

                            if (string.IsNullOrEmpty(mySql))
                                throw new Exception("MySQLConnection no está configurada.");

                            options.UseMySql(
                                mySql,
                                ServerVersion.AutoDetect(mySql)
                            );

                            break;
                        }

                    case "SqlServer":
                        {
                            var sql = configuration.GetConnectionString("SqlServerConnection");

                            if (string.IsNullOrEmpty(sql))
                                throw new Exception("SqlServerConnection no está configurada.");

                            options.UseSqlServer(sql);

                            break;
                        }

                    case "PostgreSQL":
                        {
                            var postgres = configuration.GetConnectionString("PostgresConnection");

                            if (string.IsNullOrEmpty(postgres))
                                throw new Exception("PostgresConnection no está configurada.");

                            options.UseNpgsql(postgres);

                            break;
                        }

                    default:
                        throw new InvalidOperationException(
                            $"Proveedor de base de datos no soportado: {dbProvider}"
                        );
                }
            });

            return services;
        }
    }
}