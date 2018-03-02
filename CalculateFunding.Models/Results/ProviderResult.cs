using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Versioning;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class DataRelationshipSummary : VersionReference
    {
        public DataRelationshipSummary(string id, string name, string version) : base(id, name, version)
        {
        }
    }

    public class ProviderSourceDataset : VersionContainer<SourceDataset>
    {
        [JsonProperty("specification")]
        public Reference Specification { get; set; }

        [JsonProperty("provider")]
        public Reference Provider { get; set; }


        [JsonProperty("dataRelationshipSummary")]
        public DataRelationshipSummary DataDefinition { get; set; }
    }
    public class SourceDataset : VersionedItem
    {
        [JsonProperty("provider")]
        public VersionReference Dataset { get; set; }
        public List<object> Rows { get; set; }
        [JsonProperty("provider")]
        public string Checksum { get; set; }

        public override VersionedItem Clone()
        {
            
            return new SourceDataset
            {
                PublishStatus = PublishStatus,
                Version = Version,
                Date = Date,
                Author = Author,
                Commment = Commment,
                Dataset = new VersionReference(Dataset?.Id, Dataset?.Name, Dataset?.Version),
                Rows = Rows == null ? new List<object>() : JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(Rows)),
                Checksum = Checksum
            };
        }
    }


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
    }

}