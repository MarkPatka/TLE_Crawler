using Microsoft.Extensions.Options;
using TLECrawler.Api.Modules.Interfaces;
using TLECrawler.Application.DAL;
using TLECrawler.Application.Services;
using TLECrawler.Domain.Common.Configurations;
using TLECrawler.Domain.UserModel;
using TLECrawler.Infrastructure.Services;
using TLECrawler.Infrastructure.Services.BackgroundServices;

namespace TLECrawler.Api.Modules;

public class AuthenticationModule : IModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        services
            .AddScoped<IUserService, UserService>()
            .AddScoped<IAuthenticationService, AuthenticationService>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
#if DEBUG
        endpoints.MapGet("/GetEncryptedUser", (IUserService user) =>
        {
            var res = user.EncryptUserCredentials(); 

            return Results.Ok(res);
        });
#endif
        return endpoints;
    }
}
