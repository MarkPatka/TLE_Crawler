using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TLECrawler.Application.Services;
using TLECrawler.Application.Services.Background;
using TLECrawler.Domain.Common.Configurations;
using TLECrawler.Helpers.ClassHelper;
using TLECrawler.Helpers.Converters;

namespace TLECrawler.Infrastructure.Services.BackgroundServices;

public class MonitorLoop
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IOptions<SessionSettings> _sessionSettings;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;

    public MonitorLoop(
        IBackgroundTaskQueue taskQueue,
        ILogger<MonitorLoop> logger,
        IHostApplicationLifetime applicationLifetime,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<SessionSettings> sessionSettings)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _cancellationToken = applicationLifetime.ApplicationStopping;
        _serviceScopeFactory = serviceScopeFactory;
        _sessionSettings = sessionSettings;
    }

    public void Start()
    {
        _logger.LogInformation("Start monitoring the tasks.");
        Task.Run(async () => await MonitorAsync());
    }

    private async ValueTask MonitorAsync()
    {
        _logger.LogInformation("Creating the task queue schedule");

        var intervals = _sessionSettings.Value
            .CheckHours
            .Select(TimeOnly.Parse);
        
        var timer = new CustomTimer(BuildSession, intervals);

        string message =
            "Task queue created. " +
            "Executions are scheduled for the following intervals:\n" +
           $"[{CollectionToStringConverter.Convert<TimeOnly>(intervals.ToArray())}]";

        _logger.LogInformation("{MESSAGE}", message);

        await timer.WaitForNextIterationAsync(_cancellationToken);
        while (!_cancellationToken.IsCancellationRequested) 
        {
            if (timer.Status == TimerStatus.Released)
            {
                await _taskQueue.QueueAsync(timer.GetIterationCallBack);
                _logger.LogInformation("\nNew Session started");
                await timer.WaitForNextIterationAsync(_cancellationToken);
            }
        }
    }

    private async ValueTask BuildSession(CancellationToken token)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();               
        
        var scopedProcessingService = scope.ServiceProvider
            .GetRequiredService<IIterationService>();

        await scopedProcessingService
            .StartIterationAsync(_cancellationToken);
    }
}
