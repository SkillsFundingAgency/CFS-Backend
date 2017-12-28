using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
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