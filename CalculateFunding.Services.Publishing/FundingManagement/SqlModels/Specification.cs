using Dapper.Contrib.Extensions;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("Specifications")]
    public class Specification
    {
        [ExplicitKey]
        public string SpecificationId { get; set; }

        public string SpecificationName { get; set; }

        public int FundingStreamId { get; set; }

        public int FundingPeriodId { get; set; }
    }
}
