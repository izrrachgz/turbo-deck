using Cqrs.Abstractions;

namespace Application.Modules.Deck.Commands;

public sealed record CreatePageCommand(long ProfileId) : ICommand<DeckPageModel>;
