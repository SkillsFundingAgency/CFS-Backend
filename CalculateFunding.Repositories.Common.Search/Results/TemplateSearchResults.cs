using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class TemplateSearchResults
    {
        public TemplateSearchResults()
        {
            Results = Enumerable.Empty<TemplateSearchResult>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<TemplateSearchResult> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }

    }
}
