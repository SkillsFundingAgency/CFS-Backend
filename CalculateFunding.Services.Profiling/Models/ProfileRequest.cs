using System.ComponentModel.DataAnnotations;
using CalculateFunding.Common.Extensions;

namespace CalculateFunding.Services.Profiling.Models
{
    public class ProfileRequest
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
        public string FundingStreamId { get; set; }

        [Required]
        public string FundingPeriodId { get; set; }

        [Required]
        public string FundingLineCode { get; set; }

        [Required]
        public decimal FundingValue { get; set; }

        public string ProfilePatternKey { get; set; }

        public string ProviderType { get; set; }

        public string ProviderSubType { get; set; }

        public override string ToString()
        {
            return $"{nameof(FundingStreamId)}: {FundingStreamId}, {nameof(FundingPeriodId)}: {FundingPeriodId},{nameof(FundingLineCode)}: {FundingLineCode}, {nameof(ProfilePatternKey)}: {ProfilePatternKey}";
        }
    }
}