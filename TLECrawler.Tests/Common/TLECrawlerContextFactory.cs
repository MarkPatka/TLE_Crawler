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
        string? cs = Environment.GetEnvironmentVariable("TLECRAWLER_TEST_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException(
                "Test DB connection string is not configured. " +
                "Set the TLECRAWLER_TEST_DB_CONNECTION environment variable.");
        }

        return new SqlConnection(cs);
    }
    private User InitializeUser()
    {
        string? identity = Environment.GetEnvironmentVariable("TLECRAWLER_TEST_USER_IDENTITY");
        string? password = Environment.GetEnvironmentVariable("TLECRAWLER_TEST_USER_PASSWORD");

        if (string.IsNullOrWhiteSpace(identity) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Test user credentials are not configured. " +
                "Set TLECRAWLER_TEST_USER_IDENTITY and TLECRAWLER_TEST_USER_PASSWORD environment variables.");
        }

        return new User(identity, password);
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
