using Cqrs.Abstractions;

namespace Application.Modules.Deck.Queries;

public sealed record GetProfilePagesQuery(long ProfileId) : IQuery<IReadOnlyList<DeckPageModel>>;
