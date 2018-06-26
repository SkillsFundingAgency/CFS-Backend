using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Scenarios
{
    public class ScenarioSearchResults
    {
        public ScenarioSearchResults()
        {
            Results = Enumerable.Empty<ScenarioSearchResult>();
        }

        public IEnumerable<ScenarioSearchResult> Results { get; set; }

        public int TotalCount { get; set; }
    }
}
