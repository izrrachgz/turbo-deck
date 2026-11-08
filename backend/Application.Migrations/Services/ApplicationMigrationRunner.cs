using Time.Extensions;
using SqlKata.Execution;
using System.Data.Common;
using Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Migrations.Models;
using Application.Migrations.Logging;
using Application.Migrations.Abstractions;

namespace Application.Migrations.Services
{
    internal sealed class ApplicationMigrationRunner(
        IWritableRepository repository,
        IMigrationScriptSource scriptSource,
        TimeProvider timeProvider,
        ILogger<ApplicationMigrationRunner> logger)
    {
        private const long FirstMigrationId = 1;

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MigrationScript> scripts = await scriptSource.GetScriptsAsync(
                repository.Engine,
                cancellationToken);

            logger.MigrationScanStarted(repository.Engine, scripts.Count);

            if (scripts.Count == 0)
            {
                logger.MigrationScriptsNotFound(repository.Engine);
                throw new InvalidOperationException($"No application migration scripts were found for database engine '{repository.Engine}'.");
            }

            ValidateMigrationSequence(scripts);

            long latestAvailableMigrationId = scripts[^1].MigrationId;
            long lastAppliedMigrationId = await GetLastAppliedMigrationId(cancellationToken);

            if (lastAppliedMigrationId > latestAvailableMigrationId)
            {
                logger.MigrationHistoryAhead(lastAppliedMigrationId, latestAvailableMigrationId);
                throw new InvalidOperationException(
                    $"The database migration ledger is at {lastAppliedMigrationId}, but the deployed application contains scripts only through {latestAvailableMigrationId}.");
            }

            if (lastAppliedMigrationId == latestAvailableMigrationId)
            {
                logger.MigrationsAlreadyCurrent(lastAppliedMigrationId, latestAvailableMigrationId);
                return;
            }

            long firstPendingMigrationId = lastAppliedMigrationId <= 0
                ? FirstMigrationId
                : lastAppliedMigrationId + 1;

            for (long migrationId = firstPendingMigrationId;
                migrationId <= latestAvailableMigrationId;
                migrationId++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                MigrationScript script = scripts.First(candidate => candidate.MigrationId == migrationId);

                await ApplyMigration(script, cancellationToken);
            }

            logger.MigrationScanCompleted(repository.Engine, latestAvailableMigrationId);
        }

        private void ValidateMigrationSequence(IReadOnlyList<MigrationScript> scripts)
        {
            var duplicateMigration = scripts
                .GroupBy(script => script.MigrationId)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicateMigration is not null)
            {
                throw new InvalidOperationException($"Migration ID {duplicateMigration.Key} is defined more than once for database engine '{repository.Engine}'.");
            }

            for (var index = 0; index < scripts.Count; index++)
            {
                long expectedMigrationId = FirstMigrationId + index;

                if (scripts[index].MigrationId != expectedMigrationId)
                {
                    logger.MigrationScriptMissing(expectedMigrationId);
                    throw new InvalidOperationException($"Expected application migration ID {expectedMigrationId} was not found.");
                }
            }
        }

        private async Task ApplyMigration(MigrationScript script, CancellationToken cancellationToken)
        {
            try
            {
                logger.ApplyingMigration(script.MigrationId, script.Module, script.Name);

                if (repository.Db.Connection is not DbConnection connection)
                {
                    throw new InvalidOperationException(
                        "The configured application database connection does not support asynchronous migrations.");
                }

                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }

                await using DbCommand command = connection.CreateCommand();
                command.CommandText = script.Content;
                await command.ExecuteNonQueryAsync(cancellationToken);

                // The v1 baseline creates and seeds the ledger itself.
                if (script.MigrationId > FirstMigrationId)
                {
                    await repository.Db.Query(nameof(MigrationRecord)).InsertAsync(
                        CreateMigrationRecord(script),
                        cancellationToken: cancellationToken);
                }

                logger.MigrationApplied(script.MigrationId, script.Module, script.Name);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.MigrationApplyFailed(exception, script.MigrationId, script.Module, script.Name);
                throw;
            }
        }

        private async Task<long> GetLastAppliedMigrationId(CancellationToken cancellationToken)
        {
            try
            {
                var query = repository.Db.Query(nameof(MigrationRecord))
                    .Select(nameof(MigrationRecord.MigrationId))
                    .WhereNull(nameof(MigrationRecord.DeletedUtc))
                    .OrderByDesc(nameof(MigrationRecord.MigrationId))
                    .Limit(1);

                var records = await query.GetAsync<MigrationRecord>(cancellationToken: cancellationToken);

                return records.FirstOrDefault()?.MigrationId ?? 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.MigrationLedgerUnavailable(exception);
                return 0;
            }
        }

        private object CreateMigrationRecord(MigrationScript script)
        {
            return new
            {
                script.MigrationId,
                script.Module,
                script.Name,
                script.Checksum,
                AppliedUtc = timeProvider.GetCurrentUtcDateTime(),
                DeletedUtc = (DateTime?)null
            };
        }
    }
}
