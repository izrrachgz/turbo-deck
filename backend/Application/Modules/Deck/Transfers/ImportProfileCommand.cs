using Cqrs.Abstractions;

namespace Application.Modules.Deck.Transfers;

public sealed record ImportProfileCommand(
    ProfileTransferManifest Manifest,
    IReadOnlyList<ProfileImportMedia> Media,
    long MaximumMediaSizeBytes) : ICommand<ProfileImportResultModel>;
