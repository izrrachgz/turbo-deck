using Cqrs.Abstractions;
using Domain.Modules.Files;
using Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Application.Modules.Files.Logging;

namespace Application.Modules.Files.Queries;

internal sealed class OpenFileQueryHandler(
    IReadOnlyRepository repository,
    IFileStorage storage,
    ILogger<OpenFileQueryHandler> logger)
    : IQueryHandler<OpenFileQuery, StoredFileContent?>
{
    public async Task<StoredFileContent?> Handle(OpenFileQuery request, CancellationToken cancellationToken)
    {
        var query = repository.Query(nameof(StoredFile))
            .Where(nameof(StoredFile.Id), request.Id)
            .WhereNull(nameof(StoredFile.DeletedUtc));
        StoredFile? file = await repository.FirstOrDefaultAsync<StoredFile>(query, cancellationToken);

        if (file is null)
            return null;

        Stream? stream = await storage.OpenReadAsync(file.StorageName, cancellationToken);
        if (stream is null)
            logger.FileContentMissing(file.Id);
        return stream is null ? null : new StoredFileContent(file.ToModel(), stream);
    }
}
