using Microsoft.Extensions.Options;
using TLECrawler.Api.Modules.Interfaces;
using TLECrawler.Application.DAL;
using TLECrawler.Application.Services;
using TLECrawler.Domain.Common.Configurations;
using TLECrawler.Domain.IterationModel;
using TLECrawler.Domain.UserModel;
using TLECrawler.Infrastructure.DAL;
using TLECrawler.Infrastructure.Services;

namespace TLECrawler.Api.Modules;

public class IterationModule : IModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        services
            .AddTransient<IIterationRepository, IterationRepository>()
            .AddScoped<IIterationService, IterationService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/GetLastIteration", (IIterationRepository _iterations) =>
        {
            var iteration = _iterations.GetLast();
            return Results.Ok(iteration);
        });
        endpoints.MapGet("/GetSchedule", (IOptions<SessionSettings> _options) =>
        {
            var periods = _options.Value.CheckHours
                .Select(TimeOnly.Parse)
                .ToList();

            return Results.Ok(periods);
        });
        endpoints.MapPost("/MakeNewIteration", async (IIterationService _service, IIterationRepository _iterations) =>
        {
            var cts = new CancellationTokenSource();
            try
            {
                var makeIterationTask = _service.StartIterationAsync(cts.Token);
                await makeIterationTask;

                var iteration = _iterations.GetLast();
                return Results.Ok(new { iteration.Status!.ID, iteration.TLECount});
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    $"The problem occured while executing the iteration. Reason: {ex.Message}");
            }            
        });
        return endpoints;
    }
}
