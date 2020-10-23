using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    public class PublishedProviderFundingCsvDataBuilder : TestEntityBuilder
    {
        private string _specificationId;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _providerName;
        private decimal? _totalFunding;

        public PublishedProviderFundingCsvDataBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;
            return this;
        }

        public PublishedProviderFundingCsvDataBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;
            return this;
        }

        public PublishedProviderFundingCsvDataBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;
            return this;
        }

        public PublishedProviderFundingCsvDataBuilder WithProviderName(string providerName)
        {
            _providerName = providerName;
            return this;
        }

        public PublishedProviderFundingCsvDataBuilder WithTotalFunding(decimal? totalFunding)
        {
            _totalFunding = totalFunding;
            return this;
        }

        public PublishedProviderFundingCsvData Build()
        {
            return new PublishedProviderFundingCsvData
            {
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                SpecificationId = _specificationId ?? NewRandomString(),
                ProviderName = _providerName ?? NewRandomString(),
                TotalFunding = _totalFunding ?? NewRandomNumberBetween(0, int.MaxValue),
            };
        }
    }
}