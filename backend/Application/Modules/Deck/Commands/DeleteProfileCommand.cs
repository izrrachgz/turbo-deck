using Cqrs.Abstractions;

namespace Application.Modules.Deck.Commands;

public sealed record DeleteProfileCommand(long ProfileId, string ConfirmationName) : ICommand<DeleteProfileResultModel?>;
