using System;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class FundingPeriodBuilder : TestEntityBuilder
    {
        private DateTimeOffset? _endDate;

        public FundingPeriodBuilder WithEndDate(DateTimeOffset endDate)
        {
            _endDate = endDate;

            return this;
        }
        
        public FundingPeriod Build()
        {
            return new FundingPeriod
            {
                EndDate = _endDate.GetValueOrDefault(NewRandomDateTime())
            };
        }
    }
}