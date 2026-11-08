using Microsoft.Extensions.Logging;

namespace Api.Modules.System.Logging;

internal static partial class SystemApiLog
{
    [LoggerMessage(
        EventId = SystemApiEventIds.NetworkSettingsChanged,
        Level = LogLevel.Information,
        Message = "Network settings changed. Mode: {Mode}. Port: {Port}. DiscoveryEnabled: {DiscoveryEnabled}. Restart required.")]
    public static partial void NetworkSettingsChanged(this ILogger logger, object mode, int port, bool discoveryEnabled);

    [LoggerMessage(EventId = SystemApiEventIds.LanApiKeyRotated, Level = LogLevel.Information, Message = "LAN API key rotated.")]
    public static partial void LanApiKeyRotated(this ILogger logger);

    [LoggerMessage(EventId = SystemApiEventIds.RestartRequested, Level = LogLevel.Information, Message = "Backend restart requested.")]
    public static partial void RestartRequested(this ILogger logger);

    [LoggerMessage(EventId = SystemApiEventIds.ShutdownRequested, Level = LogLevel.Information, Message = "Backend shutdown requested.")]
    public static partial void ShutdownRequested(this ILogger logger);
}
