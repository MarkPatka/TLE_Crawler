﻿using System.Text.Json;
using TLECrawler.Api.Modules.Interfaces;
using TLECrawler.Application.DAL;
using TLECrawler.Application.Services;
using TLECrawler.Domain.TLEModel;
using TLECrawler.Infrastructure.DAL;
using TLECrawler.Infrastructure.Services;

namespace TLECrawler.Api.Modules;

public class TLEModule : IModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        services
            .AddTransient<ITLERepository, TLERepository>()
            .AddScoped<ITLEService, TLEService>()
            ;

        return services;
    }
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }



}
