using System;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{

    public class CalculationVersion : VersionedItem
    {
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