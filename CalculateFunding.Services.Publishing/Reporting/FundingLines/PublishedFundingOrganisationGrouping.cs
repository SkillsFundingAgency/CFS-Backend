using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedFundingOrganisationGrouping
    {
        public OrganisationGroupResult OrganisationGroupResult { get; set; }

        public IEnumerable<PublishedFundingVersion> PublishedFundingVersions { get; set; }
    }
}
