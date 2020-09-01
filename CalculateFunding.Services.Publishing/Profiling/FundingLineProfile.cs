using CalculateFunding.Common.Models;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class FundingLineProfile
    {
        public decimal? TotalAllocation { get; set; }
        public decimal AmountAlreadyPaid { get; set; }
        public decimal? RemainingAmount { get; set; }
        public decimal? CarryOverAmount { get; set; }
        public string ProviderName { get; set; }
        public string ProfilePatternKey { get; set; }
        public Reference LastUpdatedUser { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public decimal ProfileTotalAmount { get; set; }
        public IEnumerable<ProfileTotal> ProfileTotals { get; set; }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TotalAllocation,
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
