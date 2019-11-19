using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ProviderFundingStreamStatusResponseBuilder : TestEntityBuilder
    {
        private string _fundingStreamId;
        private int _providerDraftCount;
        private int _providerApprovedCount;
        private int _providerUpdatedCount;
        private int _providerReleasedCount;

        public ProviderFundingStreamStatusResponseBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;
            return this;
        }

        public ProviderFundingStreamStatusResponseBuilder WithProviderDraftCount(int providerDraftCount)
        {
            _providerDraftCount = providerDraftCount;
            return this;
        }

        public ProviderFundingStreamStatusResponseBuilder WithProviderApprovedCount(int providerApprovedCount)
        {
            _providerApprovedCount = providerApprovedCount;
            return this;
        }

        public ProviderFundingStreamStatusResponseBuilder WithProviderUpdatedCount(int providerUpdatedCount)
        {
            _providerUpdatedCount = providerUpdatedCount;
            return this;
        }

        public ProviderFundingStreamStatusResponseBuilder WithProviderReleasedCount(int providerReleasedCount)
        {
            _providerReleasedCount = providerReleasedCount;
            return this;
        }

        public ProviderFundingStreamStatusResponse Build()
        {
            return new ProviderFundingStreamStatusResponse
            {
                FundingStreamId = _fundingStreamId,
                ProviderApprovedCount = _providerApprovedCount,
                ProviderDraftCount = _providerDraftCount,
                ProviderReleasedCount = _providerReleasedCount,
                ProviderUpdatedCount = _providerUpdatedCount
            };
        }
    }
}
