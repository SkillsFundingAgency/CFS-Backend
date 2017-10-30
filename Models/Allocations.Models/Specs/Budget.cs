using System;
using Newtonsoft.Json;

namespace Allocations.Models.Specs
{

    public class Budget : DocumentEntity
    {
        public override string Id => $"{DocumentType}-{Name}".ToSlug();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("academicYear")]
        public string AcademicYear { get; set; }

        [JsonProperty("fundingStream")]
        public string FundingStream { get; set; }

        [JsonProperty("fundingPolicies")]
        public FundingPolicy[] FundingPolicies { get; set; }

        [JsonProperty("datasetDefinitions")]
        public DatasetDefinition[] DatasetDefinitions { get; set; }

    }


    public class DatasetDefinition
    {
        [JsonProperty("id")]
        public string Id => Name.ToSlug();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("fieldDefinitions")]
        public DatasetFieldDefinition[] FieldDefinitions { get; set; }
    }

    public class DatasetFieldDefinition
    {
        [JsonProperty("id")]
        public string Id => Name.ToSlug();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("longName")]
        public string LongName { get; set; }

        [JsonProperty("type")]
        public TypeCode Type { get; set; }
    }

}

