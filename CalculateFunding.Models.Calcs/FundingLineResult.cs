using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System;

namespace CalculateFunding.Models.Calcs
{
    [Serializable]
    public class FundingLineResult
    {
        [JsonProperty("fundingLine")]
        public Reference FundingLine { get; set; }

        [JsonProperty("fundingLineFundingStreamId")]
        public string FundingLineFundingStreamId { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("exceptionType")]
        public string ExceptionType { get; set; }

        [JsonProperty("exceptionMessage")]
        public string ExceptionMessage { get; set; }

        [JsonProperty("exceptionStackTrace")]
        public string ExceptionStackTrace { get; set; }
    }
}
