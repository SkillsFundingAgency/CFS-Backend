using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderResultWithHistory
    {
        public PublishedProviderResultWithHistory()
        {
            History = Enumerable.Empty<PublishedAllocationLineResultVersion>();
        }

        public PublishedProviderResult PublishedProviderResult { get; set; }

        public IEnumerable<PublishedAllocationLineResultVersion> History { get; set; }
    }
}
