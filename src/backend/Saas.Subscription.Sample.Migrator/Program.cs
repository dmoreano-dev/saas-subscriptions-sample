using Saas.Subscription.Sample.Infrastructure.Persistence;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__appdb");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Missing ConnectionStrings__appdb.");
    return 2;
}

try
{
    Console.WriteLine("Applying database migrations...");
    await DatabaseMigrator.MigrateAsync(connectionString);
    Console.WriteLine("Database is up to date.");
    return 0;
}
catch (Exception exception)
{
    // The exception message never includes the connection string password.
    Console.Error.WriteLine($"Migration failed: {exception.GetType().Name}: {exception.Message}");
    return 1;
}
