using Dapper.Contrib.Extensions;

namespace PersonApi.Models
{
    [Table("dbo.Envelope")]
    public class PersonEnvelopeDb
    {
        [Key]
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string? CorrelationId { get; set; }
        public string? Producer { get; set; }
        public string? Consumer { get; set; }
        public string? QueueName { get; set; }
        public string? Kernel { get; set; }
        public string? Framework { get; set; }
        public DateTime Date { get; set; }
    }
}
