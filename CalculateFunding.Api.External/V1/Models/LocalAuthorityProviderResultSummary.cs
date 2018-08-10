using System;

namespace CalculateFunding.Api.External.V1.Models
{
    [Serializable]
    public class LocalAuthorityProviderResultSummary
    {
        public LocalAuthorityProviderResultSummary()
        {
            FundingPeriods = new FundingPeriodResultSummary[0];
        }

        public string Ukprn { get; set; }

        public string LAEStab { get; set; }

        public string OrganisationName { get; set; }

        public string OrganisationType { get; set; }

        public string OrganisationSubType { get; set; }

        public int EligiblePupils { get; set; }

        public decimal AllocationValue { get; set; }

        public FundingPeriodResultSummary[] FundingPeriods { get; set; }
    }
}
