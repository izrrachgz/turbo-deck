using Npgsql;
using Api.Models;
using Api.Services;
using System.Data.Common;
using System.Data.SQLite;
using Common.Enumerations;
using MySql.Data.MySqlClient;
using Microsoft.Data.SqlClient;
using Persistence.Abstractions;
using Api.Modules.Database.Logging;
using System.Collections.Concurrent;
using Application.Migrations.Extensions;

namespace Api.Modules.Database;

internal sealed class DatabaseAdministrationService(
    IBackendSettingsStore settings,
    IDatabaseRepositoryFactory repositories,
    IServiceProvider services,
    ILogger<DatabaseAdministrationService> logger)
{
    private static readonly DatabaseEngine[] SupportedEngines =
    [
        DatabaseEngine.SqLite,
        DatabaseEngine.MsSql,
        DatabaseEngine.PgSql,
        DatabaseEngine.MySql
    ];

    private readonly SemaphoreSlim _switchGate = new(1, 1);
    private readonly ConcurrentDictionary<DatabaseEngine, string> _lastErrors = new();

    public DatabaseSettingsResponse GetSettings()
    {
        DatabaseConnectionConfiguration active = settings.GetActiveDatabase();
        DatabaseEngineStateResponse[] engines = SupportedEngines.Select(engine =>
        {
            DatabaseConnectionConfiguration configured = settings.GetDatabase(engine);
            _lastErrors.TryGetValue(engine, out string? error);
            string status = error is not null
                ? "Error"
                : engine == active.Engine ? "Connected" : configured.Configured ? "Idle" : "Unconfigured";
            return new DatabaseEngineStateResponse(
                ToApiName(engine),
                GetDisplayName(engine),
                status,
                configured.Configured,
                configured.Configured ? SanitizeConnectionString(engine, configured.ConnectionString) : null,
                error);
        }).ToArray();
        return new DatabaseSettingsResponse(ToApiName(active.Engine), engines);
    }

    public async Task<DatabaseConnectionTestResponse> TestAsync(
        DatabaseConnectionRequest request,
        CancellationToken cancellationToken)
    {
        DatabaseEngine engine = ParseEngine(request.Engine);
        string connectionString = ResolveConnectionString(engine, request.ConnectionString);
        try
        {
            await TestConnectionAsync(engine, connectionString, cancellationToken);
            _lastErrors.TryRemove(engine, out _);
            logger.ConnectionTestSucceeded(engine);
            return new DatabaseConnectionTestResponse(
                true,
                "Connected",
                "Connection succeeded. The selected database is reachable.",
                SanitizeConnectionString(engine, connectionString));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            string message = TranslateConnectionError(engine, exception);
            _lastErrors[engine] = message;
            logger.ConnectionTestFailed(engine, message);
            return new DatabaseConnectionTestResponse(false, "Error", message, null);
        }
    }

    public async Task<DatabaseSwitchResponse> SwitchAsync(
        DatabaseConnectionRequest request,
        CancellationToken cancellationToken)
    {
        DatabaseEngine engine = ParseEngine(request.Engine);
        string connectionString = ResolveConnectionString(engine, request.ConnectionString);
        await _switchGate.WaitAsync(cancellationToken);
        try
        {
            await TestConnectionAsync(engine, connectionString, cancellationToken);
            IWritableRepository repository = repositories.CreateWritable(engine, connectionString);
            try
            {
                await EnsureMigrationCompatibleAsync(repository, cancellationToken);
                await services.RunApplicationMigrations(repository, cancellationToken);
            }
            finally
            {
                (repository as IDisposable)?.Dispose();
            }
            await settings.SaveDatabaseAsync(engine, connectionString, cancellationToken);
            _lastErrors.TryRemove(engine, out _);
            logger.EngineSwitched(engine);
            return new DatabaseSwitchResponse(
                ToApiName(engine),
                "Connected",
                $"{GetDisplayName(engine)} is now the active database engine.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            string message = exception is InvalidOperationException
                ? exception.Message
                : TranslateConnectionError(engine, exception);
            _lastErrors[engine] = message;
            logger.EngineSwitchFailed(engine, message);
            throw new DatabaseSwitchException(message, exception);
        }
        finally
        {
            _switchGate.Release();
        }
    }

    private async Task TestConnectionAsync(
        DatabaseEngine engine,
        string connectionString,
        CancellationToken cancellationToken)
    {
        IWritableRepository repository = repositories.CreateWritable(engine, WithShortTimeout(engine, connectionString));
        try
        {
            if (repository.Db.Connection is not DbConnection connection)
                throw new InvalidOperationException("The selected provider does not support asynchronous connections.");

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            await connection.OpenAsync(timeout.Token);
        }
        finally
        {
            (repository as IDisposable)?.Dispose();
        }
    }

    private static async Task EnsureMigrationCompatibleAsync(
        IWritableRepository repository,
        CancellationToken cancellationToken)
    {
        if (repository.Db.Connection is not DbConnection connection)
            throw new InvalidOperationException("The selected provider does not support schema inspection.");
        if (connection.State != global::System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        string tableQuery = repository.Engine switch
        {
            DatabaseEngine.SqLite => "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';",
            DatabaseEngine.MsSql => "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';",
            DatabaseEngine.PgSql => "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE';",
            DatabaseEngine.MySql => "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE';",
            _ => throw new ArgumentOutOfRangeException(nameof(repository.Engine))
        };

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = tableQuery;
        var tables = new List<string>();
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            tables.Add(reader.GetString(0));

        if (tables.Count > 0 && !tables.Contains("MigrationRecord", StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The target database already contains tables but has no Turbo Deck migration ledger. Use an empty database or a previously initialized Turbo Deck database.");
        }
    }

    private string ResolveConnectionString(DatabaseEngine engine, string? supplied)
    {
        if (!string.IsNullOrWhiteSpace(supplied))
        {
            string candidate = supplied.Trim();
            if (candidate.Length > 8192)
                throw new ArgumentException("The connection string is too long.", nameof(supplied));
            return candidate;
        }

        DatabaseConnectionConfiguration saved = settings.GetDatabase(engine);
        if (!saved.Configured)
            throw new InvalidOperationException("Enter a connection string before testing or switching this engine.");
        return saved.ConnectionString;
    }

    private static string WithShortTimeout(DatabaseEngine engine, string connectionString) => engine switch
    {
        DatabaseEngine.SqLite => new SQLiteConnectionStringBuilder(connectionString) { DefaultTimeout = 5 }.ConnectionString,
        DatabaseEngine.MsSql => new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 5 }.ConnectionString,
        DatabaseEngine.PgSql => new NpgsqlConnectionStringBuilder(connectionString) { Timeout = 5 }.ConnectionString,
        DatabaseEngine.MySql => new MySqlConnectionStringBuilder(connectionString) { ConnectionTimeout = 5 }.ConnectionString,
        _ => connectionString
    };

    private static string SanitizeConnectionString(DatabaseEngine engine, string connectionString)
    {
        try
        {
            return engine switch
            {
                DatabaseEngine.SqLite => $"SQLite · {Path.GetFileName(new SQLiteConnectionStringBuilder(connectionString).DataSource)}",
                DatabaseEngine.MsSql => Summarize(
                    "SQL Server",
                    new SqlConnectionStringBuilder(connectionString).DataSource,
                    new SqlConnectionStringBuilder(connectionString).InitialCatalog),
                DatabaseEngine.PgSql => Summarize(
                    "PostgreSQL",
                    new NpgsqlConnectionStringBuilder(connectionString).Host,
                    new NpgsqlConnectionStringBuilder(connectionString).Database),
                DatabaseEngine.MySql => Summarize(
                    "MySQL",
                    new MySqlConnectionStringBuilder(connectionString).Server,
                    new MySqlConnectionStringBuilder(connectionString).Database),
                _ => GetDisplayName(engine)
            };
        }
        catch
        {
            return $"{GetDisplayName(engine)} · configured";
        }
    }

    private static string Summarize(string provider, string? host, string? database) =>
        $"{provider} · {host ?? "host"} / {database ?? "database"}";

    private static string TranslateConnectionError(DatabaseEngine engine, Exception exception)
    {
        if (FindException<TimeoutException>(exception) is not null || FindException<OperationCanceledException>(exception) is not null)
            return "The connection timed out. Verify the host, port, VPN, and firewall settings.";
        if (FindException<SqlException>(exception) is { } sql)
        {
            if (sql.Number == 4060) return "The SQL Server database does not exist or is not accessible.";
            if (sql.Number == 18456) return "SQL Server rejected the supplied credentials.";
            return "SQL Server could not be reached. Verify the server name, port, and encryption settings.";
        }
        if (FindException<PostgresException>(exception) is { } postgres)
        {
            if (postgres.SqlState == "3D000") return "The PostgreSQL database does not exist.";
            if (postgres.SqlState == "28P01") return "PostgreSQL rejected the supplied credentials.";
        }
        if (FindException<NpgsqlException>(exception) is not null)
            return "PostgreSQL could not be reached. Verify the host, port, and SSL settings.";
        if (FindException<MySqlException>(exception) is { } mysql)
        {
            if (mysql.Number == 1049) return "The MySQL database does not exist.";
            if (mysql.Number == 1045) return "MySQL rejected the supplied credentials.";
            return "MySQL could not be reached. Verify the host, port, and TLS settings.";
        }
        if (engine == DatabaseEngine.SqLite)
            return "SQLite could not open the database file. Verify the path and file permissions.";
        return $"{GetDisplayName(engine)} rejected the connection settings.";
    }

    private static TException? FindException<TException>(Exception exception) where TException : Exception
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TException match)
                return match;
        }
        return null;
    }

    internal static DatabaseEngine ParseEngine(string engine)
    {
        if (string.IsNullOrWhiteSpace(engine))
            throw new ArgumentException("Select a database engine.", nameof(engine));

        return engine.Trim().ToLowerInvariant() switch
        {
            "sqlite" => DatabaseEngine.SqLite,
            "mssql" or "sqlserver" => DatabaseEngine.MsSql,
            "pgsql" or "postgresql" => DatabaseEngine.PgSql,
            "mysql" => DatabaseEngine.MySql,
            _ => throw new ArgumentException("The selected database engine is not supported.", nameof(engine))
        };
    }

    internal static string ToApiName(DatabaseEngine engine) => engine switch
    {
        DatabaseEngine.SqLite => "sqlite",
        DatabaseEngine.MsSql => "mssql",
        DatabaseEngine.PgSql => "pgsql",
        DatabaseEngine.MySql => "mysql",
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };

    private static string GetDisplayName(DatabaseEngine engine) => engine switch
    {
        DatabaseEngine.SqLite => "SQLite",
        DatabaseEngine.MsSql => "Microsoft SQL Server",
        DatabaseEngine.PgSql => "PostgreSQL",
        DatabaseEngine.MySql => "MySQL",
        _ => engine.ToString()
    };
}

internal sealed class DatabaseSwitchException(string message, Exception innerException) : Exception(message, innerException);
