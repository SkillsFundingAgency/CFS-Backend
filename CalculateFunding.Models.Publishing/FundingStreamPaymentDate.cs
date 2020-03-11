using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    ///     The update date on which the profile value
    ///     for the period matching update details will be
    ///     paid
    /// </summary>
    public class FundingStreamPaymentDate
    {
        [JsonProperty("year")] 
        public int Year { get; set; }

        [JsonProperty("type")] 
        public ProfilePeriodType Type { get; set; }

        [JsonProperty("typeValue")] 
        public string TypeValue { get; set; }

        [JsonProperty("occurrence")] 
        public int Occurrence { get; set; }

        [JsonProperty("date")] 
        public DateTimeOffset Date { get; set; }
    }
}