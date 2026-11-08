using Time.Extensions;
using Cqrs.Abstractions;
using SqlKata.Execution;
using Domain.Modules.Files;
using Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Files.Logging;

namespace Application.Modules.Files.Commands;

internal sealed class DeleteFileCommandHandler(
    IWritableRepository repository,
    IFileStorage storage,
    TimeProvider timeProvider,
    ILogger<DeleteFileCommandHandler> logger) : ICommandHandler<DeleteFileCommand, bool>
{
    public async Task<bool> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        StoredFile? file = await repository.Db.Query(nameof(StoredFile))
            .Where(nameof(StoredFile.Id), request.Id)
            .WhereNull(nameof(StoredFile.DeletedUtc))
            .FirstOrDefaultAsync<StoredFile>(cancellationToken: cancellationToken);

        if (file is null)
        {
            logger.FileDeleteNotFound(request.Id);
            return false;
        }

        DateTime now = timeProvider.GetCurrentUtcDateTime();
        int updated = await repository.Db.Query(nameof(StoredFile))
            .Where(nameof(StoredFile.Id), request.Id)
            .WhereNull(nameof(StoredFile.DeletedUtc))
            .UpdateAsync(new { DeletedUtc = now, ModifiedUtc = now }, cancellationToken: cancellationToken);

        if (updated == 0)
        {
            logger.FileDeleteNotFound(request.Id);
            return false;
        }

        await storage.DeleteAsync(file.StorageName, cancellationToken);
        logger.FileDeleted(request.Id);
        return true;
    }
}
