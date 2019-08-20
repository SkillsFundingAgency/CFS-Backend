using System;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class FinancialEnvelope
    {
        [JsonProperty("monthStart")]
        public Month MonthStart { get; set; }

        [JsonProperty("yearStart")]
        public int YearStart { get; set; }

        [JsonProperty("monthEnd")]
        public Month MonthEnd { get; set; }

        [JsonProperty("yearEnd")]
        public int YearEnd { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonIgnore]
        public DateTime StartDate
        {
            get => DateTime.Parse($"1 {MonthStart} {YearStart}");
        }

        [JsonIgnore]
        public DateTime EndDate
        {
            get => DateTime.Parse($"1 {MonthEnd} {YearEnd}");
        }
    }
}
