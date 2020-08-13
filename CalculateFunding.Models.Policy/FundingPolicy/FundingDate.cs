using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    /// <summary>
    /// Funding dates for a funding line for a funding period for a funding stream
    /// </summary>
    public class FundingDate : IIdentifiable
    {
        /// <summary>
        /// Comos document id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Funding Stream ID. eg PSG, DSG
        /// </summary>
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        /// <summary>
        /// Funding Period ID - Will be in the format of Funding Period Type Id-Funding Period eg AY-1920 or FY-2021
        /// </summary>
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [JsonProperty("fundingLineId")]
        public string FundingLineId { get; set; }

        [JsonProperty("patterns")]
        public IEnumerable<FundingDatePattern> Patterns { get; set; }
    }
}
