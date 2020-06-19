using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    [Serializable]
    public class CalculationResult
    {
        [JsonProperty("id")] 
        public string Id { get; set; }

        [JsonProperty("value")] 
        public object Value { get; set; }
    }
}