using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingPeriodsUpdateModelBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private DateTimeOffset _startDate;
        private DateTimeOffset _endDate;
        private string _period;
        private FundingPeriodType? _type;

        public FundingPeriodsUpdateModelBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public FundingPeriodsUpdateModelBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public FundingPeriodsUpdateModelBuilder WithStartDate(DateTimeOffset startDate)
        {
            _startDate = startDate;
            return this;
        }

        public FundingPeriodsUpdateModelBuilder WithEndDate(DateTimeOffset endDate)
        {
            _endDate = endDate;
            return this;
        }

        public FundingPeriodsUpdateModelBuilder WithPeriod(string period)
        {
            _period = period;
            return this;
        }

        public FundingPeriodsUpdateModelBuilder WithFundingType(FundingPeriodType? type)
        {
            _type = type;
            return this;
        }

        public FundingPeriodsUpdateModel Build()
        {
            return new FundingPeriodsUpdateModel
            {
                FundingPeriods = new FundingPeriod[]
                {
                    new FundingPeriod
                    {
                        Id = _id,
                        Name = _name,
                        StartDate = _startDate,
                        EndDate = _endDate,
                        Period = _period,
                        Type = _type
                    }
                }
            };
        }
    }
}
