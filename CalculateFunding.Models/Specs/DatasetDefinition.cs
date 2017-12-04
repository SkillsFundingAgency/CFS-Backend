using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class DatasetDefinition
    {
        [JsonProperty("id")]
        public string Id => Name.ToSlug();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("fieldDefinitions")]
        public List<DatasetFieldDefinition> FieldDefinitions { get; set; }

        [JsonProperty("sourceMapping")]
        public DatasetMapping SourceMapping { get; set; }
    }

    public class FieldMapping
    {
        [JsonProperty("matchColumn")]
        public string MatchColumn { get; set; }
    }

    public class DatasetMapping
    {
        [JsonProperty("sourceType")]
        public string SourceType { get; set; }
    
        [JsonProperty("academicYear")]
        public string AcademicYear { get; set; }

        [JsonProperty("matchName")]
        public string MatchName { get; set; }
    }
}