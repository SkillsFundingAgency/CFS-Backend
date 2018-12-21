using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
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
    }
}
