using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations
{
    /// <summary>
    /// NB this is a stateful component (must be scoped or transient)
    /// </summary>
    public class ProviderVariationsApplication : IApplyProviderVariations
    {
        private readonly ICollection<PublishedProvider> _providersToUpdate = new HashSet<PublishedProvider>();
        private readonly ICollection<PublishedProvider> _newProvidersToAdd = new HashSet<PublishedProvider>();
        private readonly ICollection<ProviderVariationContext> _variationContexts = new List<ProviderVariationContext>();
        
        //TODO; figure out which components are needed to successfully apply queue variations and then
        //add them as constructor dependencies to this component and then also as Get accessors on the interface IApplyProviderVariations
        public ProviderVariationsApplication(IPublishingResiliencePolicies resiliencePolicies,
            ISpecificationsApiClient specificationsApiClient)
        {
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));

            SpecificationsApiClient = specificationsApiClient;
            ResiliencePolicies = resiliencePolicies;
        }

        public IPublishingResiliencePolicies ResiliencePolicies { get; }
        
        public ISpecificationsApiClient SpecificationsApiClient { get; }

        public void AddVariationContext(ProviderVariationContext variationContext)
        {
            _variationContexts.Add(variationContext);
        }
        
        public async Task ApplyProviderVariations()
        {
            foreach (ProviderVariationContext variationContext in _variationContexts)
            {
                //TODO; figure out what the context needs to be able to apply changes and push through this method sig
                //by adding accessors to IApplyProviderVariations 
                await variationContext.ApplyVariationChanges(this);
            }
        }

        public bool HasErrors => _variationContexts.Any(_ => _.ErrorMessages.Count > 0);

        public IEnumerable<string> ErrorMessages => _variationContexts.SelectMany(_ => _.ErrorMessages).ToArray();
        
        public IEnumerable<PublishedProvider> ProvidersToUpdate => _providersToUpdate;
        
        public IEnumerable<PublishedProvider> NewProvidersToAdd => _newProvidersToAdd;

        public void AddPublishedProviderToUpdate(PublishedProvider publishedProvider)
        {
            _providersToUpdate.Add(publishedProvider);
        }

        public void AddNewProviderToAdd(PublishedProvider publishedProvider)
        {
            _newProvidersToAdd.Add(publishedProvider);
        }
    }
}