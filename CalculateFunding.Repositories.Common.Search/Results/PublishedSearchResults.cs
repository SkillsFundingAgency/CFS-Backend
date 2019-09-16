using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search.Results
{   

    public class PublishedSearchResults
    {
        public PublishedSearchResults()
        {
            Results = Enumerable.Empty<PublishedSearchResult>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<PublishedSearchResult> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }
    }
}
