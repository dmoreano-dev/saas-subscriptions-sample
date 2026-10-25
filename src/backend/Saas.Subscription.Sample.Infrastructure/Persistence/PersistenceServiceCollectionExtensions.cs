using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Saas.Subscription.Sample.Application.Authorization;
using Saas.Subscription.Sample.Application.Catalog;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Entitlements;
using Saas.Subscription.Sample.Application.Projects;
using Saas.Subscription.Sample.Infrastructure.Persistence.Authorization;
using Saas.Subscription.Sample.Infrastructure.Persistence.Catalog;
using Saas.Subscription.Sample.Infrastructure.Persistence.Projects;
using Saas.Subscription.Sample.Infrastructure.Persistence.Subscriptions;

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

        services.AddScoped<ICatalogReader, CatalogReader>();
        services.AddScoped<IAccountAccessReader, AccountAccessReader>();
        services.AddScoped<IAccountAccessService, AccountAccessService>();
        services.AddScoped<IProjectStore, ProjectStore>();
        services.AddScoped<IEntitlementReader, EntitlementReader>();
        services.AddScoped<IEntitlementService, EntitlementService>();

        return services;
    }
}
