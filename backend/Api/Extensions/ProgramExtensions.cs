using Api.Services;
using Api.Constants;
using Api.Middleware;
using Api.Configuration;
using Time.Serialization;
using Api.Modules.Realtime;
using Application.Extensions;
using Localization.Configuration;
using Application.Modules.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Application.Migrations.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Application.Infrastructure.Modules.Files;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Api.Extensions;

internal static class ProgramExtensions
{
    public static void ConfigureServices(this WebApplicationBuilder builder, BackendRuntime runtime)
    {
        IConfigurationSection api = builder.Configuration.GetRequiredSection(ApiConstants.Section);
        builder.Services.AddSingleton(runtime);

        builder.Services.AddOptions<NetworkOptions>()
            .Bind(api.GetSection("Network"))
            .Validate(options => options.Port is >= 0 and <= 65535, "The network port is invalid.")
            .Validate(options => options.Mode != NetworkMode.Lan || options.Addresses.Length != 0, "LAN mode requires an address.")
            .Validate(
                options => options.Mode != NetworkMode.Lan || options.Https.Enabled || options.AllowInsecureHttp,
                "LAN mode requires HTTPS unless insecure HTTP is explicitly enabled.")
            .ValidateOnStart();
        builder.Services.AddOptions<SecurityOptions>()
            .Bind(api.GetSection("Security"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ControlToken), "TURBO_DECK_CONTROL_TOKEN is required.")
            .ValidateOnStart();
        builder.Services.AddOptions<FilesOptions>()
            .Bind(api.GetSection("Files"))
            .Validate(options => options.MaximumFileSizeBytes > 0, "The maximum file size must be positive.")
            .ValidateOnStart();
        builder.Services.AddOptions<RealtimeOptions>()
            .Bind(api.GetSection("Realtime"))
            .Validate(options => options.MaximumReceiveMessageSizeBytes > 0, "The realtime message limit must be positive.")
            .Validate(options => options.RequestTimeoutSeconds > 0, "The realtime request timeout must be positive.")
            .ValidateOnStart();

        FilesOptions files = api.GetSection("Files").Get<FilesOptions>() ?? new FilesOptions();
        builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = files.MaximumFileSizeBytes + 1024 * 1024);

        builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.AddUtcDateTimeConverters());
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy => ConfigureCors(policy, api)));

        RealtimeOptions realtime = api.GetSection("Realtime").Get<RealtimeOptions>() ?? new RealtimeOptions();
        ISignalRServerBuilder signalR = builder.Services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = realtime.MaximumReceiveMessageSizeBytes;
            options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        });
        if (!string.IsNullOrWhiteSpace(realtime.RedisBackplaneConnectionString))
            signalR.AddStackExchangeRedis(realtime.RedisBackplaneConnectionString);

        builder.Services.ConfigureApplicationServices(api);
        builder.Services.AddFileInfrastructure(api);
        builder.Services.AddApplicationMigrations();

        builder.Services.AddSingleton<ISecurityTokenService, SecurityTokenService>();
        builder.Services.AddSingleton<IBackendSettingsStore, BackendSettingsStore>();
        builder.Services.AddSingleton<IRealtimeConnectionRegistry, RealtimeConnectionRegistry>();
        builder.Services.AddSingleton<IRealtimePublisher, SignalRRealtimePublisher>();
        builder.Services.AddHostedService<ApiStartupInitializationService>();
        builder.Services.AddHostedService<ParentProcessLifetimeService>();
        builder.Services.AddHostedService<DiscoveryHostedService>();
    }

    public static void ConfigurePipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseRouting();
        app.UseCors();
        LocalizationOptions localization = app.Services.GetRequiredService<IOptionsMonitor<LocalizationOptions>>().CurrentValue;
        app.UseRequestLocalization(options =>
        {
            options.AddSupportedCultures(localization.SupportedCultures);
            options.AddSupportedUICultures(localization.SupportedCultures);
            options.SetDefaultCulture(localization.DefaultCulture);
        });
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<ApiAccessMiddleware>();
        app.MapControllers();
        app.MapHub<RealtimeHub>(ApiConstants.RealtimeHubPath);
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();
    }

    private static void ConfigureCors(CorsPolicyBuilder policy, IConfiguration api)
    {
        NetworkOptions network = api.GetSection("Network").Get<NetworkOptions>() ?? new NetworkOptions();
        if (network.CorsAllowedOrigins.Length == 0)
            return;

        policy.WithOrigins(network.CorsAllowedOrigins.Select(origin => origin.TrimEnd('/')).ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }
}
