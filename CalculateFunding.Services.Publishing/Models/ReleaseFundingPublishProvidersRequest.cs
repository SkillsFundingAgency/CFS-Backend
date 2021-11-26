using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Models
{
    public class ReleaseFundingPublishProvidersRequest
    {
        public IEnumerable<string> PublishedProviderIds { get; set; }
        public IEnumerable<string> ChannelCodes { get; set; }
    }
}
