using Newtonsoft.Json;

namespace CalculateFunding.Services.Profiling.Models
{
    public class ProfilePatternReProfilingConfiguration
    {
        /// <summary>
        ///     Can this profile pattern configuration be re-profiled
        /// </summary>
        [JsonProperty("reProfilingEnabled")]
        public bool ReProfilingEnabled { get; set; }

        /// <summary>
        ///     The re-profiling strategy to use when the funding line amount has increased
        /// </summary>
        [JsonProperty("increasedAmountStrategyKey")]
        public string IncreasedAmountStrategyKey { get; set; }

        /// <summary>
        ///     The re-profiling strategy to use when the funding line amount has decreased
        /// </summary>
        [JsonProperty("decreasedAmountStrategyKey")]
        public string DecreasedAmountStrategyKey { get; set; }

        /// <summary>
        ///     The re-profiling strategy to use when the funding line amount has stayed the same
        /// </summary>
        [JsonProperty("sameAmountStrategyKey")]
        public string SameAmountStrategyKey { get; set; }
        
        [JsonProperty("initialFundingStrategyKey")]
        public string InitialFundingStrategyKey { get; set; }

        [JsonProperty("initialFundingStrategyWithCatchupKey")]
        public string InitialFundingStrategyWithCatchupKey { get; set; }

        [JsonProperty("initialClosureFundingStrategyKey")]
        public string InitialClosureFundingStrategyKey { get; set; }

        [JsonProperty("converterFundingStrategyKey")]
        public string ConverterFundingStrategyKey { get; set; }

        public string GetReProfilingStrategyKeyForFundingAmountChange(ReProfileRequest request)
        {
            if (request.FundingLineTotalChange == 0)
            {
                return request.ForceSameAsKey != null ? (string)GetType().GetProperty(request.ForceSameAsKey).GetValue(this) : SameAmountStrategyKey;
            }
            else if (request.FundingLineTotalChange > 0)
            {
                return IncreasedAmountStrategyKey;
            }
            else
            {
                return DecreasedAmountStrategyKey;
            }
        }
    }
}