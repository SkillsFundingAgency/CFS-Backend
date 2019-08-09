using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    /// Funding values grouped by the distribution period (envelope) they are paid in.
    /// </summary>
    public class DistributionPeriod
    {
        /// <summary>
        /// The overall value for the distribution period in pence. Rolled up from all child Funding Lines where Type = Payment
        /// </summary>
        [JsonProperty("value")]
        public decimal Value { get; set; }

        /// <summary>
        /// The funding period the funding relates to.
        /// </summary>
        [JsonProperty("distributionPeriodId")]
        public string DistributionPeriodId { get; set; }

        /// <summary>
        /// The periods that this funding line where paid in / are due to be paid in.
        /// </summary>
        [JsonProperty("profilePeriods", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<ProfilePeriod> ProfilePeriods { get; set; }
    }
}
