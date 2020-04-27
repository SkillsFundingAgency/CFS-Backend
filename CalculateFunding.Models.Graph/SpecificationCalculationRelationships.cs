using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Graph
{
    public class SpecificationCalculationRelationships
    {
        [JsonProperty("specification")]
        public Specification Specification { get; set; }
        
        [JsonProperty("calculations")]
        public IEnumerable<Calculation> Calculations { get; set; }
        
        [JsonProperty("calculationrelationships")]
        public IEnumerable<CalculationRelationship> CalculationRelationships { get; set; }

        [JsonProperty("calculationdatasetfieldrelationships")]
        public IEnumerable<CalculationDataFieldRelationship> CalculationDataFieldRelationships { get; set; }

        [JsonProperty("datasetdatafieldrelationships")]
        public IEnumerable<DatasetDataFieldRelationship> DatasetDataFieldRelationships { get; set; }

        [JsonProperty("datasetdatasetdefinitionrelationships")]
        public IEnumerable<DatasetDatasetDefinitionRelationship> DatasetDatasetDefinitionRelationships { get; set; }
    }
}