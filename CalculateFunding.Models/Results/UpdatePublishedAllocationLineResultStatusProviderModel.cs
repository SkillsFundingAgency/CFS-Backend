using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class UpdatePublishedAllocationLineResultStatusProviderModel
    {
        public string ProviderId { get; set; }

        public IEnumerable<string> AllocationLineIds { get; set; }
    }
}
