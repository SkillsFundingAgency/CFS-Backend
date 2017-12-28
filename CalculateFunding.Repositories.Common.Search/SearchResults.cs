using System.Collections.Generic;

namespace CalculateFunding.Repositories.Common.Search
{
    public class SearchResults<T>
    {
        public string SearchTerm { get; set; }
        public long? TotalCount { get; set; }
        public List<SearchResult<T>> Results { get; set; }
        public List<Facet> Facets { get; set; }
    }

    public class IndexError
    {
        public string Key { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class Facet
    {
    }
}