using System.Collections.Generic;

namespace CalculateFunding.Models.Publishing.FundingManagement
{
    public class ReleaseProvidersToChannelRequest
    {
        public IEnumerable<string> Channels { get; set; }

        public IEnumerable<string> ProviderIds { get; set; }
    }
}
