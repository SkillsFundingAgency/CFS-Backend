using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class SpecificationVersionBuilder : TestEntityBuilder
    {
        private string _specificationId;
        private string[] _fundingStreamIds = new string[0];
        private string _fundingPeriodId;

        public SpecificationVersionBuilder WithSpecificationId(string id)
        {
            _specificationId = id;

            return this;
        }

        public SpecificationVersionBuilder WithFundingStreamsIds(params string[] fundingStreamIds)
        {
            _fundingStreamIds = fundingStreamIds;

            return this;
        }

        public SpecificationVersionBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public SpecificationVersion Build()
        {
            return new SpecificationVersion
            {
                SpecificationId = _specificationId,
                FundingPeriod = NewReferenceForId(_fundingPeriodId ?? NewRandomString()),
                FundingStreams = _fundingStreamIds.Select(NewReferenceForId).ToArray()
            };
        }

        private Reference NewReferenceForId(string id)
        {
            return new Reference
            {
                Id = id
            };
        }
    }
}