using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedFunding : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id => $"funding-{Current.FundingStreamId}-{Current.FundingPeriod.Id}-{Current.GroupingReason}-{Current.OrganisationGroupTypeCode}-{Current.OrganisationGroupIdentifierValue}";

        [JsonProperty("current")]
        public PublishedFundingVersion Current { get; set; }

        [JsonProperty("partitionKey")]
        public string ParitionKey => Id;
    }
}
