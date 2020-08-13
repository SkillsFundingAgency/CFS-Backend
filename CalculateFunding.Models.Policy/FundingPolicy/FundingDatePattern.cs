using Newtonsoft.Json;
using System;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    public class FundingDatePattern
    {
        [JsonProperty("period")]
        public string Period { get; set; }
        
        [JsonProperty("paymentDate")]
        public DateTimeOffset PaymentDate { get; set; }
        
        [JsonProperty("periodYear")]
        public int PeriodYear { get; set; }
        
        [JsonProperty("occurrence")]
        public int Occurrence { get; set; }
    }
}
