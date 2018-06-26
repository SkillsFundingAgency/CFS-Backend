using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationDatasetRelationshipsSearchResults
    {
        public SpecificationDatasetRelationshipsSearchResults()
        {
            Results = Enumerable.Empty<SpecificationDatasetRelationshipsSearchResult>();
        }

        public IEnumerable<SpecificationDatasetRelationshipsSearchResult> Results { get; set; }

        public int TotalCount { get; set; }
    }
}
