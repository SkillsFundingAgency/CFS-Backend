using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class Calculation : Reference
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }

        [JsonProperty("isPublic")]
        public bool IsPublic { get; set; }

        public Calculation Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<Calculation>(json);
        }
    }
}