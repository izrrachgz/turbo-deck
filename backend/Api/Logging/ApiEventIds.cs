namespace Api.Logging;

internal static class ApiEventIds
{
    public const int BackendReady = ApiEventIdRanges.General + 0;
    public const int UnhandledException = ApiEventIdRanges.General + 1;
    public const int AccessDenied = ApiEventIdRanges.General + 2;
    public const int RealtimeOriginDenied = ApiEventIdRanges.General + 3;
    public const int ParentProcessMonitoringStarted = ApiEventIdRanges.General + 4;
    public const int ParentProcessExited = ApiEventIdRanges.General + 5;
    public const int DiscoveryStarted = ApiEventIdRanges.Discovery + 0;
    public const int DiscoveryFailed = ApiEventIdRanges.Discovery + 1;
}
