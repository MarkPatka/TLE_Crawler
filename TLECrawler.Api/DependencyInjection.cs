using Serilog;
using Serilog.Events;
using TLECrawler.Domain.Common.Configurations;
using TLECrawler.Infrastructure.Services.BackgroundServices;

namespace TLECrawler.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddConfiguration(configuration)
            .ConfigureCORSpolicy()
            .AddLogging();

        return services;
    }
    private static IServiceCollection AddConfiguration(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DataBaseSettings>(configuration.GetRequiredSection("Database"));
        services.Configure<SpaceTrackSettings>(configuration.GetRequiredSection("SpaceTrackLinks"));
        services.Configure<SessionSettings>(configuration.GetRequiredSection("SessionSettings"));
        services.Configure<UserCredentialsSettings>(configuration.GetRequiredSection("UserCredentials"));

        return services;
    }
    private static IServiceCollection AddLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Serilog", LogEventLevel.Information)
                    .WriteTo.Logger(l => 
                    {
                        l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information)
                        .WriteTo.File(
                            path: "Logs/Information/.log",
                            rollingInterval: RollingInterval.Day,
                            levelSwitch: new Serilog.Core.LoggingLevelSwitch(LogEventLevel.Information),
                            outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
                    })
                    .WriteTo.Logger(l =>
                    {
                        l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning)
                        .WriteTo.File(
                            path: "Logs/Warning/.log",
                            rollingInterval: RollingInterval.Day,
                            levelSwitch: new Serilog.Core.LoggingLevelSwitch(LogEventLevel.Warning),
                            outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
                    })
                    .WriteTo.Logger(l =>
                    {
                        l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error)
                        .WriteTo.File(
                            path: "Logs/Error/.log",
                            rollingInterval: RollingInterval.Day,
                            levelSwitch: new Serilog.Core.LoggingLevelSwitch(LogEventLevel.Error),
                            outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
                    })
                    .CreateLogger();

            builder.AddSerilog(Log.Logger);
        });
        return services;
    }
    private static IServiceCollection ConfigureCORSpolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin",
                builder => builder.WithOrigins("http://vm-tle:44393")
                                  .AllowCredentials()
                                  .WithMethods("GET", "POST")
                                  .AllowAnyHeader()
                                  .WithExposedHeaders("X-Custom-Header"));
        });
        return services;
    }
    public static WebApplication RunBackgroundTasks(this WebApplication app)
    {
        var monitor = app.Services.GetRequiredService<MonitorLoop>();
        monitor.Start();
        return app;
    }
}
