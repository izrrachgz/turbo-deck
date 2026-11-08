using Cqrs.Abstractions;

namespace Application.Modules.Files.Queries;

public sealed record GetFilesQuery(int Limit = 100) : IQuery<IReadOnlyList<StoredFileModel>>;
