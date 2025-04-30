using TLECrawler.Domain.TLEModel;

namespace TLECrawler.Application.Services;

public interface ISpaceTrackService
{
    public Task<TLE_ST[]> GetNewTLEsFromSpaceTrack(DateTime? from = null);
    public Task<HttpClient> SetSpaceTrackAccess();
    public Task<TLE_ST[]> SendGetTLERequestAsync(HttpClient httpClient, Uri requestUri);
    public Uri FormGetTLEsFromRangeRequestUrl(DateTime from, DateTime to);
    public bool IsWebSourceAvailable(string host = "8.8.8.8", int timeout = 1000);
}
