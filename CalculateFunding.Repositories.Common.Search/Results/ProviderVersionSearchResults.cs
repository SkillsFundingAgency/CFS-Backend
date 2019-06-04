using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class ProviderVersionSearchResults
    {
        public ProviderVersionSearchResults()
        {
            Results = Enumerable.Empty<ProviderVersionSearchResult>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<ProviderVersionSearchResult> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }
    }
}
