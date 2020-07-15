using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Repositories.Common.Search;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationDatasetRelationshipsSearchResults
    {
        public SpecificationDatasetRelationshipsSearchResults()
        {
            Results = Enumerable.Empty<SpecificationDatasetRelationshipsSearchResult>();
            Facets = Enumerable.Empty<Facet>();
        }
        
        public IEnumerable<Facet> Facets { get; set; }

        public IEnumerable<SpecificationDatasetRelationshipsSearchResult> Results { get; set; }

        public int TotalCount { get; set; }
    }
}
