using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedProvidersContext
    {
        public IDictionary<string, HashSet<string>> OrganisationGroupResultsData 
        { 
            get; set; 
        }

        public IEnumerable<Provider> ScopedProviders
        {
            get; set;
        }

        public IEnumerable<PublishedFunding> CurrentPublishedFunding
        {
            get; set;
        }

        public string SpecificationId
        {
            get; set;
        }

        public string ProviderVersionId
        {
            get; set;
        }

        public FundingConfiguration FundingConfiguration
        {
            get; set;
        }
    }
}
