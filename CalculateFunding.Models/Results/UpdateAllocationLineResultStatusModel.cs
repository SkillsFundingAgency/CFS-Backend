using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Results
{
    public class UpdatePublishedAllocationLineResultStatusModel
    {
        public AllocationLineStatus Status { get; set; }

        public IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers { get; set; }
    }
}
