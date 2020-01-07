using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Aggregations
{
    public class CalculationAggregation
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("values")]
        public IEnumerable<AggregateValue> Values { get; set; }
    }
}
