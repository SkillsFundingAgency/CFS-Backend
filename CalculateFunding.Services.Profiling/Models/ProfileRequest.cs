using System.ComponentModel.DataAnnotations;
using CalculateFunding.Common.Extensions;

namespace CalculateFunding.Services.Profiling.Models
{
    public class ProfileRequest : ProfileRequestBase
    {
        public ProfileRequest()
        {
        }

        public ProfileRequest(string fundingStreamId, 
            string fundingPeriodId, 
            string fundingLineCode, 
            decimal fundingValue)
        {
            FundingStreamId = fundingStreamId;
            FundingPeriodId = fundingPeriodId;
            FundingLineCode = fundingLineCode;
            FundingValue = fundingValue;
        }

        [Required]
        public decimal FundingValue { get; set; }
    }
}