using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Calcs.ObsoleteItems
{
    public class ObsoleteItem : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("itemType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ObsoleteItemType ItemType { get; set; }

        [JsonProperty("enumValueName")]
        public string EnumValueName { get; set; }

        [JsonProperty("fundingLineId")]
        public uint? FundingLineId { get; set; }
        
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("templateCalculationId")]
        public uint? TemplateCalculationId { get; set; }

        [JsonProperty("codeReference")]
        public string CodeReference { get; set; }

        [JsonProperty("calculationIds")]
        public ICollection<string> CalculationIds { get; set; }

        public bool TryAddCalculationId(string calculationId)
        {
            if (HasCalculationId(calculationId)) return false;

            CalculationIds.Add(calculationId);

            return true;
        }

        public bool TryRemoveCalculationId(string calculationId)
        {
            if (!HasCalculationId(calculationId)) return false;
            
            CalculationIds.Remove(calculationId);

            return true;
        }

        private bool HasCalculationId(string calculationId) 
            => (CalculationIds ??= new List<string>()).Contains(calculationId);

        [JsonIgnore]
        public bool IsEmpty => CalculationIds?.Any() != true;

    }
}
