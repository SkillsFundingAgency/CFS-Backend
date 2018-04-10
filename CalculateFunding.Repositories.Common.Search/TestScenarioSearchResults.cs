using CalculateFunding.Repositories.Common.Search.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Repositories.Common.Search
{
    public class TestScenarioSearchResults
    {
        public TestScenarioSearchResults()
        {
            Results = Enumerable.Empty<TestScenarioSearchResult>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<TestScenarioSearchResult> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }
    }
}
