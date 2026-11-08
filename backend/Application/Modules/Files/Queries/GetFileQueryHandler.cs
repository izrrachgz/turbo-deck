using Cqrs.Abstractions;
using Domain.Modules.Files;
using Persistence.Abstractions;

namespace Application.Modules.Files.Queries;

internal sealed class GetFileQueryHandler(IReadOnlyRepository repository) : IQueryHandler<GetFileQuery, StoredFileModel?>
{
    public async Task<StoredFileModel?> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        var query = repository.Query(nameof(StoredFile))
            .Where(nameof(StoredFile.Id), request.Id)
            .WhereNull(nameof(StoredFile.DeletedUtc));
        StoredFile? file = await repository.FirstOrDefaultAsync<StoredFile>(query, cancellationToken);

        return file?.ToModel();
    }
}
