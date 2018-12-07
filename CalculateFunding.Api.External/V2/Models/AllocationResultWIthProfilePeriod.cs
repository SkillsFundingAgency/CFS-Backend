using System;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class AllocationResultWIthProfilePeriod : AllocationResult
    {
        public AllocationResultWIthProfilePeriod()
        {
            ProfilePeriods = new ProfilePeriod[0];
        }

        public ProfilePeriod[] ProfilePeriods { get; set; }
    }
}