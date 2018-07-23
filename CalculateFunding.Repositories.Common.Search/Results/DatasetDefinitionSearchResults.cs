using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class DatasetDefinitionSearchResults
    {
        public DatasetDefinitionSearchResults()
        {
            Results = Enumerable.Empty<DatasetDefinitionSearchResult>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<DatasetDefinitionSearchResult> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }

    }
}
