using Common.Enumerations;
using Persistence.Repository;
using Persistence.Abstractions;

namespace Api.Modules.Database;

internal interface IDatabaseRepositoryFactory
{
    IWritableRepository CreateWritable();

    IReadOnlyRepository CreateReadOnly();

    IWritableRepository CreateWritable(DatabaseEngine engine, string connectionString);
}

internal sealed class DatabaseRepositoryFactory(
    Services.IBackendSettingsStore settings,
    ILoggerFactory loggerFactory) : IDatabaseRepositoryFactory
{
    public IWritableRepository CreateWritable()
    {
        Services.DatabaseConnectionConfiguration database = settings.GetActiveDatabase();
        return CreateWritable(database.Engine, database.ConnectionString);
    }

    public IReadOnlyRepository CreateReadOnly()
    {
        Services.DatabaseConnectionConfiguration database = settings.GetActiveDatabase();
        EnsureConfigured(database);
        return new ReadOnlyRepository(
            database.ConnectionString,
            database.Engine,
            loggerFactory.CreateLogger<ReadOnlyRepository>());
    }

    public IWritableRepository CreateWritable(DatabaseEngine engine, string connectionString)
    {
        var database = new Services.DatabaseConnectionConfiguration(engine, connectionString, !string.IsNullOrWhiteSpace(connectionString));
        EnsureConfigured(database);
        return new WritableRepository(connectionString, engine, loggerFactory.CreateLogger<WritableRepository>());
    }

    private static void EnsureConfigured(Services.DatabaseConnectionConfiguration database)
    {
        if (!database.Configured)
            throw new InvalidOperationException($"Database engine '{database.Engine}' does not have a configured connection string.");
    }
}
