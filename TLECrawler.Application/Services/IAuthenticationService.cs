using TLECrawler.Domain.UserModel;

namespace TLECrawler.Application.Services;

public interface IAuthenticationService
{
    public Task<HttpClient> LogInAsync(User user);
}
