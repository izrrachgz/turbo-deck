using Api.Logging;

namespace Api.Modules.Database.Logging;

internal static class DatabaseApiEventIds
{
    public const int ConnectionTestSucceeded = ApiEventIdRanges.Database + 0;
    public const int ConnectionTestFailed = ApiEventIdRanges.Database + 1;
    public const int EngineSwitched = ApiEventIdRanges.Database + 2;
    public const int EngineSwitchFailed = ApiEventIdRanges.Database + 3;
}
