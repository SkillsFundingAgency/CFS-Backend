using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProviderFundingStreamStatusResponseComparer : IEqualityComparer<ProviderFundingStreamStatusResponse>
    {
        public bool Equals(ProviderFundingStreamStatusResponse x, ProviderFundingStreamStatusResponse y)
        {
            return x.FundingStreamId == y.FundingStreamId
                && x.ProviderApprovedCount == y.ProviderApprovedCount
                && x.ProviderDraftCount == y.ProviderDraftCount
                && x.ProviderReleasedCount == y.ProviderReleasedCount
                && x.ProviderUpdatedCount == y.ProviderUpdatedCount;
        }

        public int GetHashCode(ProviderFundingStreamStatusResponse obj)
        {
            return obj.GetHashCode();
        }
    }
}
