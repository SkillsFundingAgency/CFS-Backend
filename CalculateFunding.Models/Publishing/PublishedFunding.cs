using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedFunding : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id => $"funding-{Current.GroupingReason}-{Current.OrganisationGroupTypeIdentifier}-{Current.OrganisationGroupIdentifierValue}-{Current.FundingPeriod.Id}-{Current.FundingStreamId}";

        [JsonProperty("current")]
        public PublishedFundingVersion Current { get; set; }

        [JsonProperty("partitionKey")]
        public string ParitionKey => Id;
    }
}
