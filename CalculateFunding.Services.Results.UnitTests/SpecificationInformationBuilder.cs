using System;
using CalculateFunding.Services.Results.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class SpecificationInformationBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private DateTimeOffset? _lastUpdatedDate;
        private DateTimeOffset? _fundingPeriodEndDate;
        private string _fundingPeriodId;

        public SpecificationInformationBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public SpecificationInformationBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public SpecificationInformationBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public SpecificationInformationBuilder WithLastUpdatedDate(DateTimeOffset lastUpdateDate)
        {
            _lastUpdatedDate = lastUpdateDate;

            return this;
        }

        public SpecificationInformationBuilder WithFundingPeriodEndDate(DateTimeOffset fundingPeriodEndDate)
        {
            _fundingPeriodEndDate = fundingPeriodEndDate;

            return this;
        }
        
        public SpecificationInformation Build()
        {
            return new SpecificationInformation
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                LastEditDate = _lastUpdatedDate,
                FundingPeriodEnd = _fundingPeriodEndDate
            };
        }
    }
}