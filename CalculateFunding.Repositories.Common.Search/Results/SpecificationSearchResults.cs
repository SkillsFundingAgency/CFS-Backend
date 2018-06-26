using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class SpecificationSearchResults
    {
        public SpecificationSearchResults()
        {
            Results = Enumerable.Empty<SpecificationSearchResult>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<SpecificationSearchResult> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }
    }
}
