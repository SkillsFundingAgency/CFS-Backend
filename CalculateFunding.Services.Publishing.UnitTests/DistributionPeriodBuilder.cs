using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class DistributionPeriodBuilder : TestEntityBuilder
    {
        private IEnumerable<ProfilePeriod> _profilePeriods;
        private string _distributionPeriodId;
        private decimal? _value;

        public DistributionPeriodBuilder WithValue(decimal value)
        {
            _value = value;

            return this;
        }
        
        public DistributionPeriodBuilder WithProfilePeriods(params ProfilePeriod[] profilePeriods)
        {
            _profilePeriods = profilePeriods;

            return this;
        }

        public DistributionPeriodBuilder WithDistributionPeriodId(string distributionPeriodId)
        {
            _distributionPeriodId = distributionPeriodId;

            return this;
        }

        public DistributionPeriod Build()
        {
            return new DistributionPeriod
            {
                DistributionPeriodId = _distributionPeriodId ?? NewRandomString(),
                ProfilePeriods = _profilePeriods,
                Value = _value.GetValueOrDefault(NewRandomNumberBetween(1, int.MaxValue))
            };
        }
    }
}