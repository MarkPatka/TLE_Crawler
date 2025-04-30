using Microsoft.AspNetCore.DataProtection;
using System.Data;
using System.Net.Http.Json;
using TLECrawler.Tests.Common;
using System.Net;

namespace TLECrawler.Tests;

public class AuthenticatationTest 
{
    private TLECrawlerContextFactory _context;

    public AuthenticatationTest()
    {
        _context = new TLECrawlerContextFactory();
    }
    [Fact]
    public void DecryptDatebaseCredentials_Success()
    {
        // Arrange
        string dataSourceReal = "vm-tle";
        string catalogReal = "TLE_test";
        string userReal = "tle_crawler";
        string passwordReal = "1234qweR";

        // Act        
        string protectedDataSource = _context.UserProtector.Protect(dataSourceReal);
        string protectedCatalog = _context.UserProtector.Protect(catalogReal);
        string protectedUser = _context.UserProtector.Protect(userReal);
        string protectedPassword = _context.UserProtector.Protect(passwordReal);


        string dataSourceFact = _context.UserProtector.Unprotect(protectedDataSource);
        string catalogFact = _context.UserProtector.Unprotect(protectedCatalog);
        string userFact = _context.UserProtector.Unprotect(protectedUser);
        string passwordFact = _context.UserProtector.Unprotect(protectedPassword);

        // Assert
        Assert.Equal(dataSourceFact, dataSourceReal);
        Assert.Equal(catalogFact, catalogReal);
        Assert.Equal(userFact, userReal);
        Assert.Equal(passwordFact, passwordReal);

    }
    [Fact]
    public void DatabaseAccess_Success()
    {
        // Arrange
        _context.SqlConnection.Open();
        // Act
        ConnectionState fact = _context.SqlConnection.State;
        ConnectionState expect = ConnectionState.Open;
        // Assert
        Assert.Equal(fact, expect);
    }
    [Fact]
    public void DecryptUserCredentials_Success()
    {
        // Arrange
        string passwordReal = _context.User!.Password;
        string loginReal = _context.User.Identity;

        // Act        
        string protectedPassword = _context.UserProtector.Protect(passwordReal);
        string protectedLogin = _context.UserProtector.Protect(loginReal);

        string passwordFact = _context.UserProtector.Unprotect(protectedPassword);
        string loginFact    = _context.UserProtector.Unprotect(protectedLogin);

        // Assert
        Assert.Equal(loginFact, loginReal);
        Assert.Equal(passwordFact, passwordReal);
    }
    [Fact(Skip = "to prevent request flooding")]
    public async Task SpaceTrackLogIn_Success()
    {
        // Arrange
        var request = JsonContent.Create(_context.User);

        // Act        
        using HttpResponseMessage response = await _context.HttpClient
            .PostAsync("https://www.space-track.org/ajaxauth/login", request);

        HttpStatusCode statusFact = response.StatusCode;
        HttpStatusCode statusExpect = HttpStatusCode.OK;
        var cookies = _context.Cookies.GetAllCookies();
        string? token = cookies["chocolatechip"]?.Value ?? null;

        // Assert
        Assert.Equal(statusExpect, statusFact);
        Assert.NotEmpty(cookies);
        Assert.NotNull(token);
    }
}
