using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Graph
{
    public class SpecificationCalculationRelationships
    {
        public const string ToIdField = "HasCalculation";
        public const string FromIdField = "BelongsToSpecification";

        [JsonProperty("specification")]
        public Specification Specification { get; set; }
        
        [JsonProperty("calculations")]
        public IEnumerable<Calculation> Calculations { get; set; }

        [JsonProperty("fundinglines")]
        public IEnumerable<FundingLine> FundingLines { get; set; }
        
        [JsonProperty("calculationrelationships")]
        public IEnumerable<CalculationRelationship> CalculationRelationships { get; set; }

        [JsonProperty("fundinglinerelationships")]
        public IEnumerable<FundingLineCalculationRelationship> FundingLineRelationships { get; set; }

        [JsonProperty("calculationdatasetfieldrelationships")]
        public IEnumerable<CalculationDataFieldRelationship> CalculationDataFieldRelationships { get; set; }

        [JsonProperty("datasetdatafieldrelationships")]
        public IEnumerable<DatasetDataFieldRelationship> DatasetDataFieldRelationships { get; set; }

        [JsonProperty("datasetdatasetdefinitionrelationships")]
        public IEnumerable<DatasetDatasetDefinitionRelationship> DatasetDatasetDefinitionRelationships { get; set; }

        [JsonProperty("calculationenumrelationships")]
        public IEnumerable<CalculationEnumRelationship> CalculationEnumRelationships { get; set; }

        [JsonProperty("datasetrelationshipdatafieldrelationships")]
        public IEnumerable<DatasetRelationshipDataFieldRelationship> DatasetRelationshipDataFieldRelationships { get; set; }

        [JsonProperty("datasetrelationships")]
        public IEnumerable<DatasetRelationship> DatasetRelationships { get; set; }
    }
}