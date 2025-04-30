using TLECrawler.Domain.TLEModel;

namespace TLECrawler.Application.Services;

public interface ITLEService
{
    Task<int> PersistUnic(TLE_ST[] tles, int iterationId);
}
