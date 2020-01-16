using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class ProfileVariationPointer
    {
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("fundingLineId")]
        public string FundingLineId { get; set; }

        [JsonProperty("periodType")]
        public string PeriodType { get; set; }

        [JsonProperty("typeValue")]
        public string TypeValue { get; set; }

        [JsonProperty("year")]
        public int Year { get; set; }

        [JsonProperty("occurrence")]
        public int Occurrence { get; set; }
    }
}
