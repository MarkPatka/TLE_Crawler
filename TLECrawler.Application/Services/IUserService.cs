using TLECrawler.Domain.UserModel;

namespace TLECrawler.Application.Services;

public interface IUserService
{
    public User GetUserCredentials();
    public User EncryptUserCredentials();
}
