using System;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Profiling.IntegrationTests.ReProfiling
{
    public class ProfilePeriodPatternBuilder : TestEntityBuilder
    {
        private int? _occurrence;
        private string _period;
        private PeriodType? _periodType;
        private string _distributionPeriod;
        private int? _year;
        private DateTime? _endDate;
        private DateTime? _startDate;
        private decimal? _percentage;

        public ProfilePeriodPatternBuilder WithOccurrence(int occurrence)
        {
            _occurrence = occurrence;

            return this;
        }

        public ProfilePeriodPatternBuilder WithPeriod(string period)
        {
            _period = period;

            return this;
        }

        public ProfilePeriodPatternBuilder WithType(PeriodType type)
        {
            _periodType = type;

            return this;
        }

        public ProfilePeriodPatternBuilder WithDistributionPeriod(string distributionPeriod)
        {
            _distributionPeriod = distributionPeriod;

            return this;
        }

        public ProfilePeriodPatternBuilder WithYear(int year)
        {
            _year = year;

            return this;
        }

        public ProfilePeriodPatternBuilder WithStartDate(DateTime startDate)
        {
            _startDate = startDate;

            return this;
        }

        public ProfilePeriodPatternBuilder WithEndDate(DateTime endDate)
        {
            _endDate = endDate;

            return this;
        }

        public ProfilePeriodPatternBuilder WithPercentage(decimal percentage)
        {
            _percentage = percentage;

            return this;
        }
        
        public ProfilePeriodPattern Build()
        {
            DateTime randomDate = NewRandomDateTime().DateTime;
            
            return new ProfilePeriodPattern
            {
                Occurrence = _occurrence.GetValueOrDefault(),
                Period = _period ?? NewRandomMonth(),
                PeriodType = _periodType.GetValueOrDefault(NewRandomEnum<PeriodType>()),
                DistributionPeriod = _distributionPeriod ?? NewRandomString(),
                PeriodYear = _year.GetValueOrDefault(NewRandomYear()),
                PeriodEndDate = _endDate.GetValueOrDefault(randomDate.AddDays(1)),
                PeriodStartDate = _startDate.GetValueOrDefault(randomDate),
                PeriodPatternPercentage = _percentage.GetValueOrDefault(NewRandomNumberBetween(1, 100))
            };
        } 
    }
}