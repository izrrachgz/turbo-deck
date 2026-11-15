namespace Application.Modules.Deck.Transfers;

public sealed record ProfileTransferManifest(
    int FormatVersion,
    DateTime ExportedUtc,
    ProfileTransferProfile Profile,
    IReadOnlyList<ProfileTransferMediaAsset> Media,
    IReadOnlyList<ProfileTransferRewardBinding> TwitchRewardBindings);

public sealed record ProfileTransferProfile(
    string Name,
    int SelectedPageNumber,
    IReadOnlyList<ProfileTransferPage> Pages);

public sealed record ProfileTransferPage(
    string Name,
    int Position,
    IReadOnlyList<ProfileTransferCell> Cells);

public sealed record ProfileTransferCell(
    string Key,
    int RowIndex,
    int ColumnIndex,
    string? Label,
    bool Enabled,
    string? MediaKey,
    string? ThumbnailMediaKey,
    decimal Volume,
    decimal PlaybackRate,
    decimal PitchSemitones,
    int DisplayDurationMilliseconds,
    bool DisplayPermanently,
    string DisplaySize,
    string AnimationInType,
    int AnimationInDurationMilliseconds,
    string AnimationOutType,
    int AnimationOutDurationMilliseconds,
    string QueuePriority);

public sealed record ProfileTransferMediaAsset(
    string Key,
    string ArchivePath,
    string OriginalFileName,
    string ContentType,
    string Extension,
    long Size,
    string Sha256,
    string DisplayName,
    string Kind,
    string InspectionStatus,
    int? Width,
    int? Height,
    long? DurationMilliseconds,
    string? VideoCodec,
    string? AudioCodec,
    bool HasAudio,
    string? WaveformData);

public sealed record ProfileTransferRewardBinding(
    string RewardId,
    string RewardTitle,
    string CellKey,
    bool Enabled);

public sealed record ProfileExportFile(string ArchivePath, long StoredFileId);

public sealed record ProfileExportModel(ProfileTransferManifest Manifest, IReadOnlyList<ProfileExportFile> Files);

public sealed record ProfileImportMedia(string Key, Stream Content);

public sealed record ProfileImportResultModel(ProfileModel Profile, int MediaCount, int CellCount, int TwitchBindingCount);
