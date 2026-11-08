using Api.Logging;

namespace Api.Modules.System.Logging;

internal static class SystemApiEventIds
{
    public const int NetworkSettingsChanged = ApiEventIdRanges.System + 0;
    public const int LanApiKeyRotated = ApiEventIdRanges.System + 1;
    public const int RestartRequested = ApiEventIdRanges.System + 2;
    public const int ShutdownRequested = ApiEventIdRanges.System + 3;
}
