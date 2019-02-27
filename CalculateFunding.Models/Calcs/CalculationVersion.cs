using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{

    public class CalculationVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id
        {
            get { return $"{CalculationId}_version_{Version}"; }
        }

        [JsonProperty("entityId")]
        public override string EntityId
        {
            get { return $"{CalculationId}"; }
        }

        [JsonProperty("decimalPlaces")]
        public int DecimalPlaces { get; set; }

        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }

        [JsonProperty("calculationId")]
        public string CalculationId { get; set; }

        public override VersionedItem Clone()
        {
            return new CalculationVersion
            {
                PublishStatus = PublishStatus,
                Version = Version,
                SourceCode = SourceCode,
                Date = Date,
                Author = Author,
                Comment = Comment,
                DecimalPlaces = DecimalPlaces,
                CalculationId = CalculationId
            };
        }
    }
}