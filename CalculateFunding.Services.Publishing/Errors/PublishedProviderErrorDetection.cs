using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class PublishedProviderErrorDetection : IPublishedProviderErrorDetection
    {
        private readonly IEnumerable<IDetectPublishedProviderErrors> _errorDetectors;
        private PublishedProvidersContext _publishedProvidersContext;

        public PublishedProviderErrorDetection(IEnumerable<IDetectPublishedProviderErrors> errorDetectors)
        {
            Guard.ArgumentNotNull(errorDetectors, nameof(errorDetectors));
            
            _errorDetectors = errorDetectors;
        }

        public void PreparePublishedProviders(IEnumerable<Provider> scopedProviders, string specificationId, string providerVersionId, FundingConfiguration fundingConfiguration)
        {
            _publishedProvidersContext = new PublishedProvidersContext
            {
                ScopedProviders = scopedProviders,
                SpecificationId = specificationId,
                ProviderVersionId = providerVersionId,
                OrganisationGroupResultsData = new Dictionary<string, IEnumerable<OrganisationGroupResult>>(),
                FundingConfiguration = fundingConfiguration
            };
        }

        public async Task ProcessPublishedProvider(PublishedProvider publishedProvider)
        {
            await ProcessPublishedProvider(publishedProvider, _ => true);
        }

        public async Task ProcessPublishedProvider(PublishedProvider publishedProvider, Func<IDetectPublishedProviderErrors, bool> predicate)
        {
            foreach (IDetectPublishedProviderErrors errorDetector in _errorDetectors.Where(_ => predicate(_)))
            {
                await errorDetector.DetectErrors(publishedProvider, _publishedProvidersContext);
            }
        }
    }
}