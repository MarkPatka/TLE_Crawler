using TLECrawler.Api.Modules.Interfaces;
using TLECrawler.Application.DAL;
using TLECrawler.Application.Services;
using TLECrawler.Domain.UserModel;
using TLECrawler.Infrastructure.DAL;
using TLECrawler.Infrastructure.Services;

namespace TLECrawler.Api.Modules;

public class SQLModule : IModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        services
            .AddSingleton<ITLEDBFactory, TLEDBFactory>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/SetSonnection", (ITLEDBFactory _tledb) =>
        {
            var connection = _tledb.InitializeConnection();
            return Results.Ok("DB Connection Status is: " + connection.State.ToString());
        });
#if DEBUG
        endpoints.MapGet("/GetDatabaseEncryption", (ITLEDBFactory _tledb) =>
        {
            var connection = _tledb.GetDatabaseCredentials();
            return Results.Ok(connection);
        });
#endif
        return endpoints;
    }
}
