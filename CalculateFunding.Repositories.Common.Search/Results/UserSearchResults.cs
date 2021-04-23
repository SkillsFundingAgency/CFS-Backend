using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class UserSearchResults
    {
        public UserSearchResults()
        {
            Results = Enumerable.Empty<UserSearchResult>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<UserSearchResult> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }

    }
}
