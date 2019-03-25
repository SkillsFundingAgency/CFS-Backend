using CalculateFunding.Models.Search;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models
{
    public class SearchModel
    {
        public SearchModel()
        {
            OrderBy = Enumerable.Empty<string>();
            Filters = new Dictionary<string, string[]>();
            SearchFields = Enumerable.Empty<string>();
            OverrideFacetFields = Enumerable.Empty<string>();
        }

        public int PageNumber { get; set; }

        public int Top { get; set; }

        public string SearchTerm { get; set; } = "";

        public IEnumerable<string> OrderBy { get; set; }

        public IDictionary<string, string[]> Filters { get; set; }

        public bool IncludeFacets { get; set; }

        public int FacetCount { get; set; } = 10;

        public bool CountOnly { get; set; }

        public SearchMode SearchMode { get; set; }

        public IEnumerable<string> SearchFields { get; set; }

        public IEnumerable<string> OverrideFacetFields { get; set; }
    }
}
