using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    public class PublishedProviderFundingBuilder : TestEntityBuilder
    {
        private string _specificationId;
        private string _fundingStreamId;
        private string _publishedProviderId;
        private string _providerSubType;
        private string _providerType;
        private decimal? _totalFunding;
        private string _laCode;

        public PublishedProviderFundingBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;
            return this;
        }

        public PublishedProviderFundingBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;
            return this;
        }

        public PublishedProviderFundingBuilder WithPublishedProviderId(string publishedProviderId)
        {
            _publishedProviderId = publishedProviderId;
            return this;
        }

        public PublishedProviderFundingBuilder WithProviderType(string providerType)
        {
            _providerType = providerType;
            return this;
        }

        public PublishedProviderFundingBuilder WithProviderSubType(string providerSubType)
        {
            _providerSubType = providerSubType;
            return this;
        }

        public PublishedProviderFundingBuilder WithTotalFunding(decimal totalFunding)
        {
            _totalFunding = totalFunding;
            return this;
        }

        public PublishedProviderFundingBuilder WithLaCode(string laCode)
        {
            _laCode = laCode;
            return this;
        }

        public PublishedProviderFunding Build()
        {
            return new PublishedProviderFunding
            {
                SpecificationId = _specificationId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                PublishedProviderId = _publishedProviderId ?? NewRandomString(),
                ProviderTypeSubType = new ProviderTypeSubType()
                {
                    ProviderType = _providerType ?? NewRandomString(),
                    ProviderSubType = _providerSubType ?? NewRandomString()
                },
                TotalFunding = _totalFunding ?? NewRandomNumberBetween(10000, Int32.MaxValue),
                LaCode = _laCode ?? NewRandomString()
            };
        }
    }
}