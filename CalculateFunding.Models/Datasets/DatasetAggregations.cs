using System.Collections.Generic;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetAggregations
    {
        public string SpecificationId { get; set; }

        public string DatasetRelationshipId { get; set; }

        public string DatasetDefinitionId { get; set; }

        public IEnumerable<AggregatedField> Fields { get; set; }
    }
}
