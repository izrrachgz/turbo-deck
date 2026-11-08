using Serilog;
using Api.Configuration;

namespace Api.Extensions;

internal static class LoggingExtensions
{
    private const string FileSinkPathKey = "Serilog:WriteTo:0:Args:path";

    public static void ConfigureLogging(this WebApplicationBuilder builder, BackendRuntime runtime)
    {
        string logDirectory = Path.Combine(runtime.DataDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);
        builder.Configuration[FileSinkPathKey] = Path.Combine(logDirectory, "log-.txt");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        builder.Host.UseSerilog();
    }
}
