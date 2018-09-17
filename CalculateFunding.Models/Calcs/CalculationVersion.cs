using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{

    public class CalculationVersion : VersionedItem
    {
        //AB: These 2 properties are not required yet, will be updated during the story
        [JsonProperty("id")]
        public override string Id
        {
            get { return ""; }
        }

        [JsonProperty("entityId")]
        public override string EntityId
        {
            get { return ""; }
        }

        [JsonProperty("decimalPlaces")]
        public int DecimalPlaces { get; set; }

        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }

        public override VersionedItem Clone()
        {
            return new CalculationVersion
            {
                PublishStatus = PublishStatus,
                Version = Version,
                SourceCode = SourceCode,
                Date = Date,
                Author = Author,
                Commment = Commment,
                DecimalPlaces = DecimalPlaces
            };
        }
    }
}