using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Publishing.IntegrationTests.PublishedProvider
{
    public class PublishedProviderTemplateParametersBuilder : TestEntityBuilder
    {
        private string _fundingPeriodId;
        private string _fundingStream;
        private string _providerId;
        private string _specificationId;
        private string _publishedProviderId;
        private string _providerType;
        private string _providerSubType;
        private string _laCode;
        private decimal? _totalFunding;
        private bool? _isIndicative;
        private string _status;

        public PublishedProviderTemplateParametersBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public PublishedProviderTemplateParametersBuilder WithFundingStream(string fundingStream)
        {
            _fundingStream = fundingStream;

            return this;
        }

        public PublishedProviderTemplateParametersBuilder WithProviderId(string providerId)
        {
            _providerId = providerId;

            return this;
        }

        public PublishedProviderTemplateParametersBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public PublishedProviderTemplateParametersBuilder WithPublishedProviderId(string publishedProviderId)
        {
            _publishedProviderId = publishedProviderId;

            return this;
        }

        public PublishedProviderTemplateParametersBuilder WithProviderType(string providerType)
        {
            _providerType = providerType;

            return this;
        }

        public PublishedProviderTemplateParametersBuilder WithProviderSubType(string providerSubType)
        {
            _providerSubType = providerSubType;

            return this;
        }

        public PublishedProviderTemplateParametersBuilder WithLaCode(string laCode)
        {
            _laCode = laCode;

            return this;
        }

        public PublishedProviderTemplateParametersBuilder WithTotalFunding(decimal totalFunding)
        {
            _totalFunding = totalFunding;

            return this;
        }

        public PublishedProviderTemplateParametersBuilder WithIsIndicative(bool isIndicative)
        {
            _isIndicative = isIndicative;

            return this;
        }

        public PublishedProviderTemplateParametersBuilder WithStatus(string status)
        {
            _status = status;

            return this;
        }

        public PublishedProviderTemplateParameters Build()
        {
            return new PublishedProviderTemplateParameters
            {
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStream = _fundingStream ?? NewRandomString(),
                ProviderId = _providerId ?? NewRandomString(),
                SpecificationId = _specificationId ?? NewRandomString(),
                PublishedProviderId = _publishedProviderId ?? NewRandomString(),
                ProviderType = _providerType ?? NewRandomString(),
                ProviderSubType = _providerSubType ?? NewRandomString(),
                LaCode = _laCode ?? NewRandomString(),
                TotalFunding = _totalFunding ?? NewRandomNumberBetween(0, 1000),
                IsIndicative = _isIndicative ?? NewRandomFlag(),
                Status = _status ?? NewRandomString()
            };
        }
    }
}
