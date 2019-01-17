using System;
using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class AllocationResultWIthProfilePeriod : AllocationResult
    {
        public AllocationResultWIthProfilePeriod()
        {
            ProfilePeriods = new Collection<ProfilePeriod>();
        }

        public Collection<ProfilePeriod> ProfilePeriods { get; set; }
    }
}