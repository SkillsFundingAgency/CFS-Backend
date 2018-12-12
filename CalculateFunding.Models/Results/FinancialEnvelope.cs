using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class FinancialEnvelope
    {
        [JsonProperty("monthStart")]
        public Months MonthStart { get; set; }

        [JsonProperty("yearStart")]
        public int YearStart { get; set; }

        [JsonProperty("monthEnd")]
        public Months MonthEnd { get; set; }

        [JsonProperty("yearEnd")]
        public int YearEnd { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }
    }
}
