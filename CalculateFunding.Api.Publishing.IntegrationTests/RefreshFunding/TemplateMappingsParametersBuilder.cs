using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    public class TemplateMappingsParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private string _fundingStreamId;
        private string _specificationId;

        public TemplateMappingsParametersBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;
            return this;
        }

        public TemplateMappingsParametersBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;
            return this;
        }

        public TemplateMappingsParametersBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public TemplateMappingsParameters Build()
        {
            return new TemplateMappingsParameters()
            {
                Id = _id ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                SpecificationId = _specificationId ?? NewRandomString()
            };
        }
    }
}
