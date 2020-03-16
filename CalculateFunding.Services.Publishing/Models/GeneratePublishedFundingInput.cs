using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedFundingInput
    {
        public IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> OrganisationGroupsToSave { get; set; }

        public TemplateMetadataContents TemplateMetadataContents { get; set; }

        public string TemplateVersion { get; set; }

        public Reference FundingStream { get; set; }

        public FundingPeriod FundingPeriod { get; set; }

        public PublishedFundingDates PublishingDates { get; set; }

        public string SpecificationId { get; set; }
    }
}
