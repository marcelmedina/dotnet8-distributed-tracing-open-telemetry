namespace PersonApi.Models
{
    public class PersonEnvelope
    {
        public required Person Person { get; set; }
        public string? CorrelationId { get; set; }
        public string? Producer { get; set; }
        public string? Kernel { get; set; }
        public string? Framework { get; set; }
    }
}
