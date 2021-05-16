using Dapper.Contrib.Extensions;

namespace CalculateFunding.Services.FundingDataZone.SqlModels
{
    [Table("Successors")]
    public class Successor
    {
        [Key]
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public string UKPRN { get; set; }
    }
}
