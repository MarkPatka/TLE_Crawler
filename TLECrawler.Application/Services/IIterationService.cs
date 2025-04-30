namespace TLECrawler.Application.Services;

public interface IIterationService
{
    Task StartIterationAsync(CancellationToken cancellationToken);
}
