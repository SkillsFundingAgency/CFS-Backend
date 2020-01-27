using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public class DistributionPeriodBuilder : TestEntityBuilder
    {
        private IEnumerable<ProfilePeriod> _profilePeriods;

        public DistributionPeriodBuilder WithProfilePeriods(params ProfilePeriod[] profilePeriods)
        {
            _profilePeriods = profilePeriods;

            return this;
        }
        
        public DistributionPeriod Build()
        {
            return new DistributionPeriod
            {
                ProfilePeriods = _profilePeriods,
            };
        }
    }
}