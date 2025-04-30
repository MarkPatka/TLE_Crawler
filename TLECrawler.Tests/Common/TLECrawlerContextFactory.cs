using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using System.Net;
using TLECrawler.Domain.UserModel;

namespace TLECrawler.Tests.Common;

public class TLECrawlerContextFactory : IDisposable
{
    private readonly IDataProtectionProvider _provider;

    public SqlConnection SqlConnection;
    public IDataProtector UserProtector;
    public IDataProtector SqlConnectionProtector;

    public User? User { get; set; } = default;
    public HttpClient HttpClient { get; set; }
    public CookieContainer Cookies { get; set; }

    public TLECrawlerContextFactory()
    {
        _provider = DataProtectionProvider.Create("TLECrawler");
        SqlConnection = InitializeSqlConnection();
        SqlConnectionProtector = InitializeProtector("SqlConnection");
        UserProtector = InitializeProtector("UserCredentials");
        User = InitializeUser();
        Cookies = new();
        HttpClient = InitializeHttpClient();       
    }

    private HttpClient InitializeHttpClient()
    {
        HttpClientHandler handler = new()
        {
            CookieContainer = Cookies
        };

        HttpClient client = new(handler)
        {
            BaseAddress = new Uri("https://www.space-track.org"),
            Timeout = TimeSpan.FromMinutes(1),
        };
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
    private SqlConnection InitializeSqlConnection()
    {
        string cs =
                $"Data Source=vm-tle;" +
                $"Initial Catalog=TLE_test;" +
                $"User ID=tle_crawler;" +
                $"Password=1234qweR;" +
                $"Trust Server Certificate=True;";

        return new SqlConnection(cs);
    }
    private User InitializeUser()
    {
        return new User("234t642f2d112e1@proton.me", "3048928hduhduyagdt2HH");
    }
    private IDataProtector InitializeProtector(string purpose)
    {
        return _provider.CreateProtector(purpose);
    }

    public void Dispose()
    {
        SqlConnection.Close();
        HttpClient.Dispose();
    }
}
