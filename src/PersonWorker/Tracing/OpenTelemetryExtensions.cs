using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PersonWorker.Tracing
{
    public static class OpenTelemetryExtensions
    {
        public static string Local { get; }
        public static string Kernel { get; }
        public static string Framework { get; }
        public static string ServiceName { get; } //cloud_RoleName
        public static string ServiceInstanceId { get; } //cloud_RoleInstance
        public static string ServiceVersion { get; }

        static OpenTelemetryExtensions()
        {
            Local = Environment.MachineName;
            Kernel = Environment.OSVersion.VersionString;
            Framework = RuntimeInformation.FrameworkDescription;
            ServiceName = typeof(OpenTelemetryExtensions).Assembly.GetName().Name! + "RabbitMQ";
            ServiceInstanceId = typeof(OpenTelemetryExtensions).Assembly.GetName().GetHashCode().ToString();
            ServiceVersion = typeof(OpenTelemetryExtensions).Assembly.GetName().Version!.ToString();
        }

        public static ActivitySource CreateActivitySource() =>
            new ActivitySource(ServiceName, ServiceVersion);

        public static IServiceCollection AddOpenTelemetryPilars(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var aspireAgentHost = configuration["Aspire:AgentHost"];
            var aspireAgentPort = configuration["Aspire:AgentPort"];
            var jaegerAgentHost = configuration["Jaeger:AgentHost"];
            var jaegerAgentPort = configuration["Jaeger:AgentPort"];
            var appInsightsConnString = configuration["AppInsightsConnectionString"];

            var openTelemetryResourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
                            serviceInstanceId: OpenTelemetryExtensions.ServiceInstanceId,
                            serviceVersion: OpenTelemetryExtensions.ServiceVersion);

            serviceCollection.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource(OpenTelemetryExtensions.ServiceName)
                    .SetResourceBuilder(openTelemetryResourceBuilder)
                    .AddSqlClientInstrumentation(options => options.SetDbStatementForText = true)
                    .AddOtlpExporter(exporter => exporter.Endpoint = new Uri($"http://{aspireAgentHost}:{aspireAgentPort}"))
                    .AddJaegerExporter(exporter =>
                    {
                        exporter.AgentHost = jaegerAgentHost;
                        exporter.AgentPort = Convert.ToInt32(jaegerAgentPort);
                    })
                    .AddAzureMonitorTraceExporter(endpoint => endpoint.ConnectionString = appInsightsConnString);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(openTelemetryResourceBuilder)
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(exporter => exporter.Endpoint = new Uri($"http://{aspireAgentHost}:{aspireAgentPort}"))
                    .AddAzureMonitorMetricExporter(endpoint => endpoint.ConnectionString = appInsightsConnString);
            });

            LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(logs =>
                {
                    logs
                        .SetResourceBuilder(openTelemetryResourceBuilder)
                        .AddOtlpExporter(exporter => exporter.Endpoint = new Uri($"http://{aspireAgentHost}:{aspireAgentPort}"))
                        .AddAzureMonitorLogExporter(endpoint => endpoint.ConnectionString = appInsightsConnString);
                });
            });

            return serviceCollection;
        }
    }
}
