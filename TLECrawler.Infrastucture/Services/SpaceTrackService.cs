using Microsoft.Extensions.Options;
using TLECrawler.Application.Services;
using TLECrawler.Domain.Common.Configurations;
using TLECrawler.Domain.TLEModel;
using System.Text.Json;
using TLECrawler.Domain.UserModel;
using System.Net.NetworkInformation;
using TLECrawler.Application.DAL;
using Microsoft.Extensions.Logging;

namespace TLECrawler.Infrastructure.Services;

public class SpaceTrackService : ISpaceTrackService
{
    private readonly IOptions<SessionSettings> _sessionSettings;
    private readonly IOptions<SpaceTrackSettings> _spacetrackUrls;
    private readonly ITLERepository _tleRepository;
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<SpaceTrackService> _logger;

    public SpaceTrackService(
        IOptions<SessionSettings> sessionSettings,
        IOptions<SpaceTrackSettings> spacetrackSettings,
        IUserService userService,
        IAuthenticationService authenticationService,
        ITLERepository tleRepository,
        ILogger<SpaceTrackService> logger)
    {
        _sessionSettings = sessionSettings;
        _spacetrackUrls = spacetrackSettings;
        _userService = userService;
        _authenticationService = authenticationService;
        _tleRepository = tleRepository;
        _logger = logger;
    }
    
    public async Task<TLE_ST[]> GetNewTLEsFromSpaceTrack(DateTime? from = null)
    {
        if (from == null)
        {
            from = _tleRepository
                .GetDateTimeOfLastUploadedTLE();
        }

        Uri request = FormGetTLEsFromRangeRequestUrl(
            from.Value, from.Value.AddDays(1));

        try
        {
            var httpClient = await SetSpaceTrackAccess();

            TLE_ST[] response = await SendGetTLERequestAsync(
                httpClient, request);

            return response;
        }
        catch (Exception ex) 
        {
            string msg = "Failed to retrieve data from Space-Track.org";
            _logger.LogError(ex, "{MSG}", msg);
            throw new TLEParseException(msg, ex);
        }
    }
    public async Task<HttpClient> SetSpaceTrackAccess()
    {
        //string host = new Uri(_spacetrackUrls.Value.BaseURL).Host;
        //if (!IsWebSourceAvailable(host))
        //{
        //    throw new Exception("Failed to reach SpaceTrack.org");
        //}

        User credentials = _userService.GetUserCredentials();
        HttpClient initializedHttpClient;
        try
        {
            initializedHttpClient = await _authenticationService
                .LogInAsync(credentials);
        }
        catch (Exception ex)
        {
            string msg = "Authentication at Space-Track.org failed";
            _logger.LogError(ex, "{MSG}", msg);
            throw new Exception(ex.Message, ex.InnerException);
        }
        return initializedHttpClient;
    }
    public Uri FormGetTLEsFromRangeRequestUrl(DateTime from, DateTime to)
    {      
        string fromUtc = 
            from.ToString("yyyy-MM-dd") + "%20" + 
            string.Format("{0:hh\\:mm\\:ss}", from.TimeOfDay);

        string toUtc = 
            to.ToString("yyyy-MM-dd") + "%20" +
            string.Format("{0:hh\\:mm\\:ss}", from.TimeOfDay);

        string url = _spacetrackUrls.Value.DataURL;
        string fromRange = fromUtc + "--" + toUtc;

        return new Uri($"{url}{fromRange}");
    }
    public async Task<TLE_ST[]> SendGetTLERequestAsync(HttpClient httpClient, Uri requestUri)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        try
        {
            response.EnsureSuccessStatusCode();

            string? responseString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TLE_ST[]>(responseString) ?? [];
        }
        catch (HttpRequestException ex)
        {
            string reason = 
                $"The possible reason is: \"{response.ReasonPhrase}\". ";
            
            string fullMessage =
                $"SpaceTrack.org authentication has failed. " +
                (string.IsNullOrEmpty(response.ReasonPhrase) ? string.Empty : reason) +
                $"For more details see inner exception: ";

            _logger.LogError(ex, "{MSG}", fullMessage);
            
            throw new Exception(fullMessage, ex);
        }
        catch (ArgumentNullException ex)
        {
            string fullMessage = $"SpaceTrack.org has returned NULL response";
            _logger.LogError(ex, "{MSG}", fullMessage);

            throw new Exception(fullMessage, ex.InnerException);
        }
        catch (Exception ex)
        {
            string fullMessage = "Deserialization exception occured while processing SpaceTrack.org response. ";
            _logger.LogError(ex, "{MSG}", fullMessage);
            throw new Exception(fullMessage, ex);
        }
    }
    /// <summary>
    /// Pings Google's public DNS server by default 
    /// (If Google will be banned in Russia change on another DNS server)
    /// </summary>
    /// <param name="host"></param>
    /// <param name="timeout"></param>
    /// <returns>
    ///     <paramref name="True"/> - if the Internet access to the source can be established, 
    ///     otherwise - <paramref name="False"/>
    /// </returns>
    public bool IsWebSourceAvailable(string host = "8.8.8.8", int timeout = 3000)
    {
        try
        {
            using Ping ping = new();

            PingReply reply = ping.Send(host, timeout);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            // LOGGING ("Error checking internet connection: " + ex.Message);
            return false;
        }
    }
}
