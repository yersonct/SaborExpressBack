using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;

namespace SaborExpress.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"] ?? "PostgreSQL";
        if (!string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo PostgreSQL dispone de migraciones verificadas. Configure DatabaseProvider=PostgreSQL.");
        var connection = configuration.GetConnectionString("PostgresConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("ConnectionStrings:PostgresConnection no está configurada.");
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection,
            npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        return services;
    }
}
