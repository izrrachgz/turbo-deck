using Cqrs.Abstractions;

namespace Application.Modules.Deck.Queries;

public sealed record GetProfilesQuery : IQuery<IReadOnlyList<ProfileModel>>;
