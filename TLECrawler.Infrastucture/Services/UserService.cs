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

    public User EncryptUserCredentials()
    {
        string identity= _protector
            .Protect("234t642f2d112e1@proton.me");

        string password = _protector
            .Protect("3048928hduhduyagdt2HH");

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
