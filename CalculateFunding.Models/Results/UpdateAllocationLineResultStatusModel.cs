using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class UpdatePublishedAllocationLineResultStatusModel
    {
        public UpdatePublishedAllocationLineResultStatusModel()
        {
            Providers = Enumerable.Empty<UpdatePublishedAllocationLineResultStatusProviderModel>();
        }

        public AllocationLineStatus Status { get; set; }

        public IEnumerable<UpdatePublishedAllocationLineResultStatusProviderModel> Providers { get; set; }
    }
}
