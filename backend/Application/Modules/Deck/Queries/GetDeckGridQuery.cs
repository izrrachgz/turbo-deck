using Cqrs.Abstractions;

namespace Application.Modules.Deck.Queries;

public sealed record GetDeckGridQuery(long PageId) : IQuery<DeckGridModel?>;
