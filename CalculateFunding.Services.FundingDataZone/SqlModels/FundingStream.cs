
using Dapper.Contrib.Extensions;

namespace CalculateFunding.Services.FundingDataZone.SqlModels
{
    [Table("FundingStream")]
    public class FundingStream
    {
        [Key]
        public int FundingStreamId { get; set; }

        public string FundingStreamName { get; set; }

        public string FundingStreamCode { get; set; }
    }
}
