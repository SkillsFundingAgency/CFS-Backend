using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Results
{

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
