using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationCreateModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string SpecificationId { get; set; }
        
        [JsonIgnore]
        public string SpecificationName { get; set; }

        public string FundingStreamId { get; set; }

        [JsonIgnore]
        public string FundingStreamName { get; set; }
        
        public CalculationValueType? ValueType { get; set; }

        [JsonIgnore]
        public CalculationType? CalculationType { get; set; }

        public string SourceCode { get; set; }

        public string Description { get; set; }
    }
}
