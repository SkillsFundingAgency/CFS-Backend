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
        
        /// <summary>
        ///     Is this configuration meant to support new opener or
        ///  allocations mid year
        /// </summary>
        [JsonProperty("initialFundingStrategyKey")]
        public string InitialFundingStrategyKey { get; set; }

        public string GetReProfilingStrategyKeyForFundingAmountChange(decimal change)
        {
            if (change == 0)
            {
                return SameAmountStrategyKey;
            }
            else if (change > 0)
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