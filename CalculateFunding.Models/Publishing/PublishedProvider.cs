using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedProvider : IIdentifiable
    {
        public string Id =>
            $"publishedprovider_{Current.ProviderId}_{Current.FundingPeriodId}_{Current.FundingStreamId}";

        public PublishedProviderVersion Current { get; set; }

        public string ParitionKey =>
            $"publishedprovider_{Current.ProviderId}_{Current.FundingPeriodId}_{Current.FundingStreamId}";
    }
}