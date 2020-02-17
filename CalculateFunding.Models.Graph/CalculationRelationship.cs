using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Graph
{
    public class CalculationRelationship
    {
        [JsonProperty("calculationoneid")]
        public string CalculationOneId { get; set; }
        
        [JsonProperty("calculationtwoid")]
        public string CalculationTwoId { get; set; }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }
        
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            return HashCode.Combine(CalculationOneId, CalculationTwoId);
        }
    }
}