using Cqrs.Abstractions;
using Domain.Modules.Files;
using Persistence.Abstractions;

namespace Application.Modules.Files.Queries;

internal sealed class OpenFileQueryHandler(IReadOnlyRepository repository, IFileStorage storage)
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
        return stream is null ? null : new StoredFileContent(file.ToModel(), stream);
    }
}
