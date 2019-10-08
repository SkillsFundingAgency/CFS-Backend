using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Generators.Schema10
{
    public class PublishedFundingIdGenerator : IPublishedFundingIdGenerator
    {
        public string GetFundingId(PublishedFundingVersion publishedFunding)
        {
            return $"{publishedFunding.FundingStreamId}-{publishedFunding.FundingPeriod.Id}-{publishedFunding.GroupingReason}-{publishedFunding.OrganisationGroupTypeCode}-{publishedFunding.OrganisationGroupIdentifierValue}-{publishedFunding.MajorVersion}_{publishedFunding.MinorVersion}";
        }
    }
}
