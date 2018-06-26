using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search
{
    public class SearchResults<TIndexType>
    {
        public string SearchTerm { get; set; }
        public long? TotalCount { get; set; }
        public List<SearchResult<TIndexType>> Results { get; set; }
        public List<Facet> Facets { get; set; }
    }
}