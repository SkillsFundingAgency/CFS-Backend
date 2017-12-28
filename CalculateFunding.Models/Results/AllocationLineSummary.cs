using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class AllocationLineSummary : ResultSummary
    {
        [JsonProperty("id")]
        public string Id { get; }
        [JsonProperty("name")]
        public string Name { get; }



        public AllocationLineSummary(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
