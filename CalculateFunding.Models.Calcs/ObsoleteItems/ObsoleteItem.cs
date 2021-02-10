using System.Collections.Generic;
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

        [JsonProperty("templateCalculationId")]
        public uint? TemplateCalculationId { get; set; }

        [JsonProperty("codeReference")]
        public string CodeReference { get; set; }

        [JsonProperty("calculationIds")]
        public IEnumerable<string> CalculationIds { get; set; }

    }
}
