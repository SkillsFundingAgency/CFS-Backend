using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedProviderVersionBuilder : TestEntityBuilder
    {
        private string _providerId;
        private string _fundingPeriodId;
        private string _fundingStreamId;
        private int? _version;
        private string _specificationId;
        private PublishedProviderStatus? _status;
        private Provider _provider;

        public PublishedProviderVersionBuilder WithProvider(Provider provider)
        {
            _provider = provider;

            return this;
        }

        public PublishedProviderVersionBuilder WithPublishedProviderStatus(PublishedProviderStatus status)
        {
            _status = status;

            return this;
        }
        
        public PublishedProviderVersionBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public PublishedProviderVersionBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public PublishedProviderVersionBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public PublishedProviderVersionBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public PublishedProviderVersionBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public PublishedProviderVersion Build()
        {
            return new PublishedProviderVersion
            {
                SpecificationId = _specificationId ?? NewRandomString(),
                ProviderId = _providerId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                Version = _version ?? 1,
                Status = _status.GetValueOrDefault(NewRandomEnum<PublishedProviderStatus>()),
                Provider = _provider
            };
        }
    }
}
