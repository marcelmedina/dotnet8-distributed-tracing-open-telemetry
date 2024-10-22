using System.Diagnostics;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using PersonWorker.Data;
using PersonWorker.Tracing;
using PersonApi.Models;

namespace PersonWorker
{
    public class Worker : BackgroundService
    {
        private readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly PersonRepository _repository;
        private readonly string _queueName;
        private readonly int _activeWorkerMessageInterval;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, PersonRepository repository)
        {
            _logger = logger;
            _configuration = configuration;
            _repository = repository;

            _queueName = _configuration["RabbitMQ:Queue"]!;
            _activeWorkerMessageInterval = Convert.ToInt32(configuration["ActiveWorkerMessageInterval"]);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Queue = {_queueName}");
            _logger.LogInformation("Waiting for messages...");

            var factory = new ConnectionFactory()
            {
                Uri = new Uri(_configuration["RabbitMQ:ConnectionString"]!)
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += ReceiveMessage;
            channel.QueueDeclare(_queueName);
            channel.BasicConsume(queue: _queueName,
                autoAck: true,
                consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Active Worker: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                await Task.Delay(_activeWorkerMessageInterval, stoppingToken);
            }
        }

        private void ReceiveMessage(object? sender, BasicDeliverEventArgs e)
        {
            // Solution reference:
            // https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/examples/MicroserviceExample

            // PropagationContext is extracted to identify the message headers
            var parentContext = Propagator.Extract(default, e.BasicProperties, ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;

            // Semantic convention - OpenTelemetry messaging specification:
            // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#span-name
            var activityName = $"{e.RoutingKey} receive";

            using var activity = OpenTelemetryExtensions.CreateActivitySource()
                .StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext);

            var messageContent = Encoding.UTF8.GetString(e.Body.ToArray());
            _logger.LogInformation($"[{_queueName} | New message] " + messageContent);
            activity?.SetTag("message", messageContent);
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination_kind", "queue");
            activity?.SetTag("messaging.destination", _configuration["RabbitMQ:Exchange"]);
            activity?.SetTag("messaging.rabbitmq.routing_key", _queueName);

            PersonEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<PersonEnvelope>(messageContent,
                    new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deserialization has failed.");
                activity?.SetStatus(ActivityStatusCode.Error);
                envelope = null;
            }

            if (envelope is not null)
            {
                try
                {
                    _repository.Save(envelope);
                    _logger.LogInformation("Envelope has been successfully saved!");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error has occurred: {ex.Message}");
                    activity?.SetStatus(ActivityStatusCode.Error);
                }
            }
        }

        private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
        {
            try
            {
                if (props.Headers.TryGetValue(key, out var value))
                {
                    var bytes = value as byte[];
                    return new[] { Encoding.UTF8.GetString(bytes!) };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while extracting trace context: {ex.Message}");
            }

            return Enumerable.Empty<string>();
        }
    }
}