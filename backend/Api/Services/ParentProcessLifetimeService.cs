using System.Diagnostics;

namespace Api.Services;

internal sealed class ParentProcessLifetimeService(IConfiguration configuration, IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!int.TryParse(configuration["ParentPid"], out int parentPid) || parentPid <= 0)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            if (!IsRunning(parentPid))
            {
                lifetime.StopApplication();
                return;
            }
        }
    }

    private static bool IsRunning(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
