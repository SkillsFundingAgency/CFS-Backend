using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedFundingOrganisationGrouping
    {
        public PublishedFunding PublishedFunding { get; set; }
        public OrganisationGroupResult OrganisationGroupResult { get; set; }
    }
}
