using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Services.Profiling.Models
{
    public abstract class ProfileRequestBase
    {
        [Required]
        public string FundingStreamId { get; set; }

        [Required]
        public string FundingPeriodId { get; set; }

        [Required]
        public string FundingLineCode { get; set; }

        public string ProfilePatternKey { get; set; }

        public string ProviderType { get; set; }

        public string ProviderSubType { get; set; }

        public override string ToString()
        {
            return $"{nameof(FundingStreamId)}: {FundingStreamId}, {nameof(FundingPeriodId)}: {FundingPeriodId},{nameof(FundingLineCode)}: {FundingLineCode}, {nameof(ProfilePatternKey)}: {ProfilePatternKey}, {nameof(ProviderType)}: {ProviderType}, {nameof(ProviderSubType)}: {ProviderSubType}";
        }
    }
}