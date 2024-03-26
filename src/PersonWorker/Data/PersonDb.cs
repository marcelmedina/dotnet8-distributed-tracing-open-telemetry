using Dapper.Contrib.Extensions;

namespace PersonApi.Models
{
    [Table("dbo.Person")]
    public class PersonDb
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime Date { get; set; }
    }
}
