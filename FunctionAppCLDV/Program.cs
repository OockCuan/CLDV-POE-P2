using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.IIS;         
using Microsoft.AspNetCore.Server.Kestrel.Core; 
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        cfg.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        cfg.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((ctx, services) =>
    {
       
        services.Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true);
        services.Configure<IISServerOptions>(o => o.AllowSynchronousIO = true);
    })
    .Build();

host.Run();
