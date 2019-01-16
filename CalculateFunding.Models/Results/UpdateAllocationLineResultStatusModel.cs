using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models.Results
{
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
