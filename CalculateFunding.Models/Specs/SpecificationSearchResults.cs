using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationSearchResults
    {
        public SpecificationSearchResults()
        {
            Results = Enumerable.Empty<SpecificationSearchResult>();
        }

        public IEnumerable<SpecificationSearchResult> Results { get; set; }

        public int TotalCount { get; set; }
    }
}
