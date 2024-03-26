using PersonWorker;
using PersonWorker.Data;
using PersonWorker.Tracing;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<PersonRepository>();

        // Open Telemetry
        services.AddOpenTelemetryPilars(hostContext.Configuration);

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();