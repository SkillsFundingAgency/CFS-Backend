using CalculateFunding.Common.Models;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class FundingLineProfile
    {
        public string FundingLineCode { get; set; }
        public string FundingLineName { get; set; }
        public decimal? TotalAllocation { get; set; }
        public decimal AmountAlreadyPaid { get; set; }
        public decimal? RemainingAmount { get; set; }
        public decimal? CarryOverAmount { get; set; }
        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public string UKPRN { get; set; }
        public string ProfilePatternKey { get; set; }
        public string ProfilePatternName { get; set; }
        public string ProfilePatternDescription { get; set; }
        public Reference LastUpdatedUser { get; set; }
        public DateTimeOffset? LastUpdatedDate { get; set; }
        public decimal? ProfileTotalAmount { get; set; }
        public IEnumerable<ProfileTotal> ProfileTotals { get; set; }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                TotalAllocation,
                AmountAlreadyPaid,
                RemainingAmount,
                CarryOverAmount,
                ProviderName,
                ProfilePatternKey,
                LastUpdatedUser,
                LastUpdatedDate);
        }

    }
}
