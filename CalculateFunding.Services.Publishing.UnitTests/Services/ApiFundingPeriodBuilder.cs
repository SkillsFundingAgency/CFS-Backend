using System;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    public class ApiFundingPeriodBuilder : TestEntityBuilder
    {
        private DateTimeOffset? _startDate;
        private DateTimeOffset? _endDate;

        public ApiFundingPeriodBuilder WithStartDate(DateTimeOffset startDate)
        {
            _startDate = startDate;

            return this;
        }
        
        public ApiFundingPeriodBuilder WithEndDate(DateTimeOffset endDate)
        {
            _endDate = endDate;

            return this;
        }
        
        public FundingPeriod Build()
        {
            return new FundingPeriod
            {
                StartDate = _startDate.GetValueOrDefault(NewRandomDateTime()),
                EndDate = _endDate.GetValueOrDefault(NewRandomDateTime())
            };
        }
    }
}