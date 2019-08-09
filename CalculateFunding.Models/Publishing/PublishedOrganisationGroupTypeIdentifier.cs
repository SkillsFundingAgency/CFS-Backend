using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedOrganisationGroupTypeIdentifier
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
