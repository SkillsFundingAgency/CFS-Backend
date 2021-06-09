using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Builders
{
    public class EligibleSpecificationReferenceBuilder : TestEntityBuilder
    {
        private string _specificationId;
        private string _specificationName;
        private string _fundingPeriodId;
        private string _fundingPeriodName;
        private string _fundingStreamId;
        private string _fundingStreamName;

        public EligibleSpecificationReferenceBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public EligibleSpecificationReferenceBuilder WithSpecificationName(string specificationName)
        {
            _specificationName = specificationName;

            return this;
        }

        public EligibleSpecificationReferenceBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public EligibleSpecificationReferenceBuilder WithFundingPeriodName(string fundingPeriodName)
        {
            _fundingPeriodName = fundingPeriodName;

            return this;
        }

        public EligibleSpecificationReferenceBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public EligibleSpecificationReferenceBuilder WithFundingStreamName(string fundingStreamName)
        {
            _fundingStreamName = fundingStreamName;

            return this;
        }


        public EligibleSpecificationReference Build() => new EligibleSpecificationReference
        {
            FundingPeriodId = _fundingPeriodId,
            FundingPeriodName = _fundingPeriodName,
            FundingStreamId = _fundingStreamId,
            FundingStreamName = _fundingStreamName,
            SpecificationId = _specificationId,
            SpecificationName = _specificationName
        };
        
    }
}
