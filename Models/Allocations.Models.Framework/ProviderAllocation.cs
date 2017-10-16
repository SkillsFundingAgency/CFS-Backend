using Newtonsoft.Json;

namespace Allocations.Models.Framework
{
    public class ProviderAllocation
    {


        public ProviderAllocation(string allocationId, decimal value)
        {
            this.AllocationId = allocationId;
            this.Value = value;
        }

        [JsonProperty("id")]
        public string Id => $"{ModelName}-{AllocationId}-{URN}";
        public string ModelName { get; set; }
        public string AllocationId { get; set; }
        public string URN { get; set; }
        public decimal Value { get; set; }
    }
}