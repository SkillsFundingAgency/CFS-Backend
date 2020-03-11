using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    ///     Container for the update (payment dates)
    ///     for the profile periods in a funding stream
    /// </summary>
    public class FundingStreamPaymentDates : IIdentifiable
    {
        [JsonProperty("id")] 
        public string Id => $"{FundingStreamId}-{FundingPeriodId}";

        [JsonProperty("fundingStreamId")] 
        public string FundingStreamId { get; set; }

        [JsonProperty("fundingPeriodId")] 
        public string FundingPeriodId { get; set; }

        [JsonProperty("paymentDates")] 
        public ICollection<FundingStreamPaymentDate> PaymentDates { get; set; } = new List<FundingStreamPaymentDate>();
    }
}