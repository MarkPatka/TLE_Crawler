using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using TLECrawler.Application.Services;
using TLECrawler.Domain.Common.Configurations;
using TLECrawler.Domain.UserModel;
using TLECrawler.Infrastructure.Services.BackgroundServices;

namespace TLECrawler.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IOptions<SpaceTrackSettings> _spaceTrackUrls;

    private readonly HttpClient _httpClient;

    public AuthenticationService(
        IOptions<SpaceTrackSettings> spaceTrackUrls,
        HttpClient httpClient)
    {
        _spaceTrackUrls = spaceTrackUrls;
        _httpClient = httpClient;
    }


    /// <summary>
    /// Authenticate user credentials on SpaceTrack.org and returns access token in cookies
    /// </summary>
    /// <returns>HttpClient with initialized cookies container which has an access token to provide further operations</returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<HttpClient> LogInAsync(User user)
    {
        var request = JsonContent.Create(user);
        
        using HttpResponseMessage response = await _httpClient
            .PostAsync(_spaceTrackUrls.Value.AuthURL, request);

        try
        {
            response.EnsureSuccessStatusCode();
            return _httpClient;
        }
        catch (HttpRequestException ex)
        {
            string reason = $"The possible reason is: \"{response.ReasonPhrase}\". ";

            throw new Exception(
                $"SpaceTrack.org authentication has failed. " +
                (string.IsNullOrEmpty(response.ReasonPhrase) ? string.Empty : reason) +
                $"For more details see inner exception: ", ex);
        }
    }
}
