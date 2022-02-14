using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Models
{
    public class GeneratedPublishedFunding
    {
        public PublishedFunding PublishedFunding { get; set; }

        public PublishedFundingVersion PublishedFundingVersion { get; set; }

        public OrganisationGroupResult OrganisationGroupResult { get; set; }
    }
}
