using Domain;
using CrossCutting;
using Microsoft.Extensions.Hosting;
using Serverless_Api.Middlewares;

var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        services.AddEventStore();
        services.AddDomainDependencies();
    })
    .ConfigureFunctionsWorkerDefaults(builder => builder.UseMiddleware<AuthMiddleware>())
    .Build();

host.Run();
