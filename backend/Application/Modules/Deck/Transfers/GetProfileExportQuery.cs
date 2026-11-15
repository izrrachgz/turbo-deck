using Cqrs.Abstractions;

namespace Application.Modules.Deck.Transfers;

public sealed record GetProfileExportQuery(long ProfileId) : IQuery<ProfileExportModel?>;
