using Dapper.Contrib.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("FundingPeriods")]
    public class FundingPeriod
    {
        [Dapper.Contrib.Extensions.Key]
        public int FundingPeriodId { get; set; }

        [Required, StringLength(16)]
        public string FundingPeriodCode { get; set; }

        [Required, StringLength(128)]
        public string FundingPeriodName { get; set; }
    }
}
