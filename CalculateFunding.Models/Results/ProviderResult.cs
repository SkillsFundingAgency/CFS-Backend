using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class ProviderResult : IIdentifiable
    {
	    [JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("specification")]
        public SpecificationSummary Specification { get; set; }

		[JsonProperty("provider")]
        public ProviderSummary Provider { get; set; }

        [JsonProperty("calcResults")]
        public List<CalculationResult> CalculationResults { get; set; }

	    [JsonProperty("allocationLineResults")]
	    public List<AllocationLineResult> AllocationLineResults { get; set; }

		[JsonProperty("sourceDatasets")]
        public List<object> SourceDatasets { get; set; }
    }

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