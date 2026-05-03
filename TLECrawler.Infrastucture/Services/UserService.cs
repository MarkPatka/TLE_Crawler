using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using TLECrawler.Application.Services;
using TLECrawler.Domain.Common.Configurations;
using TLECrawler.Domain.UserModel;

namespace TLECrawler.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IOptions<UserCredentialsSettings> _userCredentials;
    private readonly IDataProtector _protector;

    public UserService(
        IOptions<UserCredentialsSettings> userCredentials,
        IDataProtectionProvider protector)
    {
        _userCredentials = userCredentials;
        _protector = protector
            .CreateProtector("UserCredentials");
    }

    public User EncryptUserCredentials(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        string identity = _protector.Protect(user.Identity);
        string password = _protector.Protect(user.Password);

        return new User(identity, password);
    }


    public User GetUserCredentials()
    {
        var credentials = _userCredentials.Value;

        string password = _protector
            .Unprotect(credentials.Password);

        string identity = _protector
            .Unprotect(credentials.Login);

        return new User(identity, password);
    }
}
