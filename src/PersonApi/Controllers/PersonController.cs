using PersonApi.Messaging;
using PersonApi.Models;
using PersonApi.Tracing;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace PersonApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersonController : Controller
    {
        private readonly ILogger<PersonController> _logger;
        private readonly IConfiguration _configuration;
        private readonly MessageSender _messageSender;

        public PersonController(ILogger<PersonController> logger,
        IConfiguration configuration, MessageSender messageSender)
        {
            _logger = logger;
            _configuration = configuration;
            _messageSender = messageSender;
        }

        [HttpPost]
        public IActionResult Post([FromBody] Person person)
        {
            _logger.LogInformation("Processing Person...");

            string correlationId = Guid.NewGuid().ToString();

            using var activity = OpenTelemetryExtensions.CreateActivitySource().StartActivity("PersonalInfo");
            activity?.SetTag("personName", person.Name);
            activity?.SetTag("correlationId", correlationId);

            var envelope = new PersonEnvelope()
            {
                Person = person,
                CorrelationId = correlationId,
                Producer = OpenTelemetryExtensions.ServiceName,
                Kernel = Environment.OSVersion.VersionString,
                Framework = RuntimeInformation.FrameworkDescription
            };

            _logger.LogInformation("Registering Person...");
            _messageSender.SendMessage<PersonEnvelope>(envelope);

            return Ok();
        }
    }
}
