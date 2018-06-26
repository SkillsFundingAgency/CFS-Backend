using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class UpdateProviderResultsModel
    {
        [JsonProperty("calcResults")]
        public List<CalculationResult> CalculationResults { get; set; }

        [JsonProperty("allocationLineResults")]
        public List<AllocationLineResult> AllocationLineResults { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

}