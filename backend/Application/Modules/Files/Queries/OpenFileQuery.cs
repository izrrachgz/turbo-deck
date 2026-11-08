using Cqrs.Abstractions;

namespace Application.Modules.Files.Queries;

public sealed record OpenFileQuery(long Id) : IQuery<StoredFileContent?>;
