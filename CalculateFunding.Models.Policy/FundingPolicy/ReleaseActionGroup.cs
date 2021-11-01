using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    public class ReleaseActionGroup : Reference
    {
        /// <summary>
        /// Sort order for the UI
        /// </summary>
        [JsonProperty("sortOrder")]
        public int SortOrder { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// List of channel codes - human readable key for identifying the channel eg Contracts or Statements.
        /// This code should match the created channel code in the publishing microservice when releasing funding.
        /// When this release action is selected, the following channels will be released to
        /// </summary>
        [JsonProperty("channelCodes")]
        public IEnumerable<string> ChannelCodes { get; set; }
    }
}
