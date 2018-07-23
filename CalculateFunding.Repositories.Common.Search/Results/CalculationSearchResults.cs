using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class CalculationSearchResults
    {
        public CalculationSearchResults()
        {
            Results = Enumerable.Empty<CalculationSearchResult>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<CalculationSearchResult> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }
    }
}
