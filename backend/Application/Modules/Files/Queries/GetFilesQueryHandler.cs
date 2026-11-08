using Cqrs.Abstractions;
using Domain.Modules.Files;
using Persistence.Abstractions;

namespace Application.Modules.Files.Queries;

internal sealed class GetFilesQueryHandler(IReadOnlyRepository repository)
    : IQueryHandler<GetFilesQuery, IReadOnlyList<StoredFileModel>>
{
    public async Task<IReadOnlyList<StoredFileModel>> Handle(GetFilesQuery request, CancellationToken cancellationToken)
    {
        var query = repository.Query(nameof(StoredFile))
            .WhereNull(nameof(StoredFile.DeletedUtc))
            .OrderByDesc(nameof(StoredFile.CreatedUtc))
            .Limit(request.Limit);
        IEnumerable<StoredFile> files = await repository.GetAsync<StoredFile>(query, cancellationToken);

        return files.Select(file => file.ToModel()).ToArray();
    }
}
