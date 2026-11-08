using Cqrs.Abstractions;

namespace Application.Modules.Files.Queries;

public sealed record GetFileQuery(long Id) : IQuery<StoredFileModel?>;
