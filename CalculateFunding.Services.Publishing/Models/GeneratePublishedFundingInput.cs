using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;

namespace CalculateFunding.Models.Publishing
{
    public class GeneratePublishedFundingInput
    {
        public IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> OrganisationGroupsToSave { get; set; }

        public TemplateMetadataContents TemplateMetadataContents { get; set; }

        public IEnumerable<PublishedProvider> PublishedProviders { get; set; }

        public string TemplateVersion { get; set; }

        public Reference FundingStream { get; set; }

        public FundingPeriod FundingPeriod { get; set; }

        public PublishedFundingDates PublishingDates { get; set; }

        public string SpecificationId { get; set; }
    }
}
