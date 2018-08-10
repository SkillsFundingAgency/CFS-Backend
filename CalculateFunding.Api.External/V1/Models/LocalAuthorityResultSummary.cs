using System;

namespace CalculateFunding.Api.External.V1.Models
{
    [Serializable]
    public class LocalAuthorityResultSummary
    {
        public LocalAuthorityResultSummary()
        {
            Providers = new LocalAuthorityProviderResultSummary[0];
        }

        public string LANo { get; set; }

        public string LAName { get; set; }

        public LocalAuthorityProviderResultSummary[] Providers { get; set; }

        public decimal TotalAllocation { get; set; }
    }
}
