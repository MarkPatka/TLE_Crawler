using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TLECrawler.Application.DAL;
using TLECrawler.Application.Services;
using TLECrawler.Application.Services.Background;
using TLECrawler.Infrastructure.DAL;
using TLECrawler.Infrastructure.Services;
using TLECrawler.Infrastructure.Services.BackgroundServices;

namespace TLECrawler.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, ConfigurationManager configuration)
    {
        services
            .AddRepositories()
            .AddServices()
            .ConfigureHttpClient(configuration)
            .AddBackgroundService();

        return services;
    }

    private static void AddBackgroundService(this IServiceCollection services)
    {
        services
            .AddSingleton<MonitorLoop>()
            .AddHostedService<QueuedHostedService>()
            .AddSingleton<IBackgroundTaskQueue>(ctx =>
            {
                var queueCapacity = 10;
                return new BackgroundTaskQueue(queueCapacity);
            });
    }
    private static IServiceCollection AddRepositories(this IServiceCollection services) 
    {
        return services;
    }
    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services
            .AddScoped<ISpaceTrackService, SpaceTrackService>()
            .AddScoped<ITLEService, TLEService>()
            ;

        return services;
    }
    private static IServiceCollection ConfigureHttpClient(
        this IServiceCollection services, ConfigurationManager configuration)
    {
        string httpClientName = configuration["SpaceTrackClient"]!;
        string url = configuration.GetSection("SpaceTrackLinks")["BaseURL"]!;

        services.AddHttpClient<IAuthenticationService, AuthenticationService>(
            httpClientName,
            client =>
            {
                client.BaseAddress = new Uri(url);
                client.Timeout = TimeSpan.FromMinutes(5);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(30));

        return services;
    }
}
