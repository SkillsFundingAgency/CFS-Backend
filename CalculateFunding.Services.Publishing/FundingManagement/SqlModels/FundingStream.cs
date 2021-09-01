using Dapper.Contrib.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("FundingStreams")]
    public class FundingStream
    {
        [Dapper.Contrib.Extensions.Key]
        public int FundingStreamId { get; set; }

        [Required, StringLength(16)]
        public string FundingStreamCode { get; set; }

        [Required, StringLength(128)]
        public string FundingStreamName { get; set; }
    }
}
