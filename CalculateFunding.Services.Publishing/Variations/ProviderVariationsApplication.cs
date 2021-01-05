using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
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
        private readonly ICollection<ProviderVariationContext> _variationContexts = new List<ProviderVariationContext>();
        private readonly ICollection<PublishedProvider> _newProvidersToAdd = new HashSet<PublishedProvider>();

        public ProviderVariationsApplication(IPublishingResiliencePolicies resiliencePolicies,
            ISpecificationsApiClient specificationsApiClient,
            IPoliciesApiClient policiesApiClient,
            ICacheProvider cacheProvider,
            IProfilingApiClient profilingApiClient,
            IReProfilingRequestBuilder reProfilingRequestBuilder,
            IReProfilingResponseMapper reProfilingResponseMapper)
        {
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.SpecificationsApiClient, "resiliencePolicies.SpecificationsApiClient");
            Guard.ArgumentNotNull(resiliencePolicies.PoliciesApiClient, "resiliencePolicies.PoliciesApiClient");
            Guard.ArgumentNotNull(resiliencePolicies.CacheProvider, "resiliencePolicies.CacheProvider");
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(profilingApiClient, nameof(profilingApiClient));
            Guard.ArgumentNotNull(reProfilingRequestBuilder, nameof(reProfilingRequestBuilder));
            Guard.ArgumentNotNull(reProfilingResponseMapper, nameof(reProfilingResponseMapper));
            
            SpecificationsApiClient = specificationsApiClient;
            ResiliencePolicies = resiliencePolicies;
            PoliciesApiClient = policiesApiClient;
            CacheProvider = cacheProvider;
            ProfilingApiClient = profilingApiClient;
            ReProfilingRequestBuilder = reProfilingRequestBuilder;
            ReProfilingResponseMapper = reProfilingResponseMapper;
        }

        public bool HasVariations => _variationContexts.AnyWithNullCheck();

        public IPublishingResiliencePolicies ResiliencePolicies { get; }
        
        public ISpecificationsApiClient SpecificationsApiClient { get; }
        
        public IPoliciesApiClient PoliciesApiClient { get; }
        
        public IProfilingApiClient ProfilingApiClient { get; }

        public ICacheProvider CacheProvider { get; }
        
        public IReProfilingRequestBuilder ReProfilingRequestBuilder { get; }
        
        public IReProfilingResponseMapper ReProfilingResponseMapper { get; }

        public void AddVariationContext(ProviderVariationContext variationContext)
        {
            _variationContexts.Add(variationContext);
        }
        
        public async Task ApplyProviderVariations()
        {
            foreach (ProviderVariationContext variationContext in _variationContexts)
            {
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