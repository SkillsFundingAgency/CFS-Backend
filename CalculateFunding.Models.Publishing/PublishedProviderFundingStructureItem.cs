using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedProviderFundingStructureItem
    {
        public PublishedProviderFundingStructureItem()
        {
        }

        public PublishedProviderFundingStructureItem(
            int level,
            string name,
            string calculationId,
            PublishedProviderFundingStructureType type,
            string value,
            string calculationType,
            List<PublishedProviderFundingStructureItem> fundingStructureItems)
        {
            Level = level;
            Name = name;
            CalculationId = calculationId;
            Type = type;
            Value = value;
            CalculationType = calculationType;
            FundingStructureItems = fundingStructureItems;
        }
        
        [JsonProperty("level")]
        public int Level { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("calculationId")]
        public string CalculationId { get; set; }
        
        [JsonProperty("type")]
        public PublishedProviderFundingStructureType Type { get; set; }
        
        [JsonProperty("value")]
        public string Value { get; set; }
        
        [JsonProperty("calculationType")]
        public string CalculationType { get; set; }
        
        [JsonProperty("fundingStructureItems")]
        public ICollection<PublishedProviderFundingStructureItem> FundingStructureItems { get; set; }
    }
}