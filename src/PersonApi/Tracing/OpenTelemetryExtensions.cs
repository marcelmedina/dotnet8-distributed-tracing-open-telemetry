using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace PersonApi.Tracing
{
    public static class OpenTelemetryExtensions
    {
        public static string ServiceName { get; } //cloud_RoleName
        public static string ServiceInstanceId { get; } //cloud_RoleInstance
        public static string ServiceVersion { get; }

        static OpenTelemetryExtensions()
        {
            ServiceName = typeof(OpenTelemetryExtensions).Assembly.GetName().Name! + "RabbitMQ";
            ServiceInstanceId = typeof(OpenTelemetryExtensions).Assembly.GetName().GetHashCode().ToString();
            ServiceVersion = typeof(OpenTelemetryExtensions).Assembly.GetName().Version!.ToString();
        }

        public static ActivitySource CreateActivitySource() =>
            new ActivitySource(ServiceName, ServiceVersion);

        public static IServiceCollection AddOpenTelemetryPilars(this IServiceCollection serviceCollection, IConfiguration configuration, ILoggingBuilder loggingBuilder)
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
                        .AddOtlpExporter(exporter => exporter.Endpoint = new Uri($"http://{aspireAgentHost}:{aspireAgentPort}"))
                        .AddJaegerExporter(exporter =>
                        {
                            exporter.AgentHost = jaegerAgentHost;
                            exporter.AgentPort = Convert.ToInt32(jaegerAgentPort);
                        });
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .SetResourceBuilder(openTelemetryResourceBuilder)
                        .AddMeter("Microsoft.AspNetCore.Hosting")
                        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                        .AddMeter("System.Net.Http")
                        .AddOtlpExporter(exporter => exporter.Endpoint = new Uri($"http://{aspireAgentHost}:{aspireAgentPort}"));
                })
                .UseAzureMonitor(endpoint => endpoint.ConnectionString = appInsightsConnString);

            loggingBuilder.AddOpenTelemetry(logs =>
            {
                logs
                    .SetResourceBuilder(openTelemetryResourceBuilder)
                    .AddOtlpExporter(exporter => exporter.Endpoint = new Uri($"http://{aspireAgentHost}:{aspireAgentPort}"));
            });

            return serviceCollection;
        }
    }
}