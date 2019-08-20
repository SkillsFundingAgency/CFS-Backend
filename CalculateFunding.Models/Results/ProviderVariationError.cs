using System;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class ProviderVariationError
    {
        public string UKPRN { get; set; }

        public string Error { get; set; }

        public string AllocationLineId { get; set; }
    }
}
