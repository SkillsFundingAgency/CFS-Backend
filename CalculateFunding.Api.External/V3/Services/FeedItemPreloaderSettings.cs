using CalculateFunding.Api.External.V3.Interfaces;

namespace CalculateFunding.Api.External.V3.Services
{
    public class FeedItemPreLoaderSettings : IFeedItemPreloaderSettings
    {
        public bool ShouldPreLoad { get; set; } = true;

        public int PreLoadCount { get; set; } = 1000;

        public int PageSize { get; set; } = 500;
    }
}