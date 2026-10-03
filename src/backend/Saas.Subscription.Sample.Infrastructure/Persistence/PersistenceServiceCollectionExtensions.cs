using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Saas.Subscription.Sample.Application.Configuration;

namespace Saas.Subscription.Sample.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AppDbContext"/> from the validated <c>Database</c> options. The options are read when
    /// the first context is created, so test hosts can still override the connection string.
    /// </summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((provider, builder) =>
        {
            var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString)
            {
                MaxPoolSize = database.MaxPoolSize,
            }.ConnectionString;

            builder
                .UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(database.CommandTimeoutSeconds))
                .UseSnakeCaseNamingConvention();
        });

        return services;
    }
}
