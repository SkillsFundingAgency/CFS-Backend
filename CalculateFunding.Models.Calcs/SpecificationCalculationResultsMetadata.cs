using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class SpecificationCalculationResultsMetadata
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("lastUpdated")]
        public DateTime LastUpdated { get; set; }
    }
}