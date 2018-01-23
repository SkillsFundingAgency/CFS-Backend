using System.Collections.Generic;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class CalculationSearchResults
    {
        public int TotalCount { get; set; }

        public IEnumerable<CalculationSearchResult> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }
    }
}
