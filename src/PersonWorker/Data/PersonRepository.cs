using Microsoft.Data.SqlClient;
using Dapper.Contrib.Extensions;
using PersonApi.Models;

namespace PersonWorker.Data;

public class PersonRepository
{
    private readonly IConfiguration _configuration;

    public PersonRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Save(PersonEnvelope envelope)
    {
        using var connection = new SqlConnection(_configuration.GetConnectionString("Person"));

        // TODO: Transaction
        var personId = connection.Insert<PersonDb>(new()
        {
            Name = envelope.Person.Name,
            Age = envelope.Person.Age,
            Email = envelope.Person.Email,
            Address = envelope.Person.Address,
            Date = DateTime.UtcNow
        });

        connection.Insert<PersonEnvelopeDb>(new()
        {
            Date = DateTime.UtcNow,
            PersonId = (int) personId,
            CorrelationId = envelope.CorrelationId,
            Producer = envelope.Producer,
            Consumer = Environment.MachineName,
            QueueName = _configuration["RabbitMQ:Queue"],
            Kernel = envelope.Kernel,
            Framework = envelope.Framework
        });
    }
}