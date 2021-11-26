using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Publishing
{
    public class ReleaseFundingPublishedProvidersSummary
    {
        public ReleaseFundingPublishedProvidersSummary()
        {
            ChannelFundings = Enumerable.Empty<ChannelFunding>();
        }

        public int TotalProviders { get; set; }
        public int TotalIndicativeProviders { get; set; }
        public decimal? TotalFunding { get; set; }
        public IEnumerable<ChannelFunding> ChannelFundings { get; set; }
    }
}
