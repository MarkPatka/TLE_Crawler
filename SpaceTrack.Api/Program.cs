using TLECrawler.Api.Modules.Extensions;
using TLECrawler.Api;
using TLECrawler.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
{
    builder
        .Services
        .AddEndpointsApiExplorer()
        .AddSwaggerGen()
        .AddMemoryCache()
        .AddDataProtection()
            .SetApplicationName("TLECrawler")
            .PersistKeysToFileSystem(new DirectoryInfo(@".\AppData"))
            .SetDefaultKeyLifetime(TimeSpan.FromDays(365))
            ;

    builder
        .Services
        .AddPresentation(builder.Configuration)
        .AddInfrastructure(builder.Configuration);

    builder
        .Services
        .RegisterModules();
}

var app = builder.Build();
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowSpecificOrigin");
    app.UseHttpsRedirection();
    app.MapEndpoints();
    app.RunBackgroundTasks();
    app.Run();
}

//static X509Certificate2 GetCertificate()
//{
//    var assembly = Assembly.GetExecutingAssembly();
//    var resource = assembly
//        .GetManifestResourceNames()
//        .First(cert => cert.EndsWith("certificate.pem"));

//    using var stream = assembly.GetManifestResourceStream(resource);
//    ArgumentNullException.ThrowIfNull(stream);

//    var bytes = new byte[stream.Length];
//    stream.Read(bytes, 0, bytes.Length);
//    return new X509Certificate2(bytes);
//}



