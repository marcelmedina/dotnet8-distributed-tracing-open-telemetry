using PersonApi.Tracing;
using PersonApi.Messaging;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry documentation
// https://opentelemetry.io/docs/instrumentation/net/getting-started/

// Open Telemetry exporters:
// https://opentelemetry.io/docs/instrumentation/net/exporters/

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Open Telemetry
builder.Services.AddOpenTelemetryPilars(builder.Configuration, builder.Logging);

builder.Services.AddScoped<MessageSender>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();