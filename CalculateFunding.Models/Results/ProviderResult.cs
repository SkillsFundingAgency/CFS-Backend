using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CalculateFunding.Models.Datasets;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
	public class ProviderResult : Reference
    {
        [JsonProperty("spec")]
        public Reference Specification { get; set; }

	    [JsonProperty("period")]
	    public Reference Period { get; set; }

		[JsonProperty("provider")]
        public ProviderSummary Provider { get; set; }

        [JsonProperty("calcResults")]
        public List<CalculationResult> CalculationResults { get; set; }

	    [JsonProperty("allocationLineResults")]
	    public List<CalculationResult> AllocationLineResults { get; set; }

		[JsonProperty("sourceDatasets")]
        public List<object> SourceDatasets { get; set; }
    }

}