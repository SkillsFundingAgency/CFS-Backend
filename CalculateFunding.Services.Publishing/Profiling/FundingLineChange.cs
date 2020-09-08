using CalculateFunding.Common.Models;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class FundingLineChange
    {
        public decimal? FundingLineTotal { get; set; }
        public decimal? PreviousFundingLineTotal { get; set; }
        public string FundingStreamName { get; set; }
        public string FundingLineName { get; set; }
        public decimal? CarryOverAmount { get; set; }
        public Reference LastUpdatedUser { get; set; }
        public DateTimeOffset? LastUpdatedDate { get; set; }
        public IEnumerable<ProfileTotal> ProfileTotals { get; set; }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FundingLineTotal,
                PreviousFundingLineTotal,
                FundingStreamName,
                FundingLineName,
                CarryOverAmount,
                LastUpdatedUser,
                LastUpdatedDate);
        }

    }
}
