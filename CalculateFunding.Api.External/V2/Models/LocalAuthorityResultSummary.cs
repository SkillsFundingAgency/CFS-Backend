using System;
using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class LocalAuthorityResultSummary
    {
        public LocalAuthorityResultSummary()
        {
            Providers = new Collection<LocalAuthorityProviderResultSummary>();
        }

        public string LANo { get; set; }

        public string LAName { get; set; }

        public Collection<LocalAuthorityProviderResultSummary> Providers { get; set; }

        public decimal TotalAllocation { get; set; }
    }
}
