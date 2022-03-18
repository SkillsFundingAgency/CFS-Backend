using System.Collections.Generic;

namespace CalculateFunding.Models.Publishing.FundingManagement
{
    public class ReleaseProvidersToChannelRequest
    {
        /// <summary>
        /// Channel codes to release providers to
        /// </summary>
        public IEnumerable<string> Channels { get; set; }

        /// <summary>
        /// Published provider IDs eg 1619-AS-2122-10000345
        /// </summary>
        public IEnumerable<string> ProviderIds { get; set; }
    }
}
