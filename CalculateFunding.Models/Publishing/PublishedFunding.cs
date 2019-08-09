using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedFunding : IIdentifiable
    {
        public string Id => $"funding_{Current.OrganisationGroupTypeIdentifier}_{Current.OrganisationGroupIdentifierValue}_{Current.FundingPeriod.Id}_{Current.FundingStreamId}";

        public PublishedFundingVersion Current { get; set; }

        public string ParitionKey => Id;
    }
}
