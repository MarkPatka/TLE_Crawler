using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TLECrawler.Application.Services.Background;

namespace TLECrawler.Infrastructure.Services.BackgroundServices;

public class QueuedHostedService : BackgroundService
{
    private readonly ILogger<QueuedHostedService> _logger;

    public IBackgroundTaskQueue TaskQueue { get; }

    public QueuedHostedService(IBackgroundTaskQueue taskQueue,
        ILogger<QueuedHostedService> logger)
    {
        TaskQueue = taskQueue;
        _logger = logger;
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await TaskQueue.DequeueAsync(stoppingToken);
            
            _logger.LogInformation("New task received");

            try
            {
                await workItem(stoppingToken);

                _logger.LogInformation("Task has completed\n");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex,
                    "Operation cancelled. " +
                    "Operation: {WorkItem}", workItem.Method.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Operation {0} failed. Reason: {1}", workItem.Method.Name, ex.Message);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            $"Queued Hosted Service is running");

        await BackgroundProcessing(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Queued Hosted Service is stopping");

        await base.StopAsync(stoppingToken);
    }
}
