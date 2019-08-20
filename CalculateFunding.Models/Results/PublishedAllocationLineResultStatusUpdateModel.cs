using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class PublishedAllocationLineResultStatusUpdateModel
    {
        public PublishedAllocationLineResultStatusUpdateModel()
        {
            Providers = new Dictionary<string, string[]>();
        }

        public AllocationLineStatus Status { get; set; }

        public Dictionary<string, string[]> Providers { get; set; }
    }
}
