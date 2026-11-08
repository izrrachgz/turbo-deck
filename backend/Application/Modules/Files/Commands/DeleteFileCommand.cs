using Cqrs.Abstractions;

namespace Application.Modules.Files.Commands;

public sealed record DeleteFileCommand(long Id) : ICommand<bool>;
