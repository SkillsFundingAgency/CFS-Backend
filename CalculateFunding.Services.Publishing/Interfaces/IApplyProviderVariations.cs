using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IApplyProviderVariations
    {
        IPublishingResiliencePolicies ResiliencePolicies { get; }
        
        ISpecificationsApiClient SpecificationsApiClient { get; }
        
        IPoliciesApiClient PoliciesApiClient { get; }
        
        IProfilingApiClient ProfilingApiClient { get; }
        
        ICacheProvider CacheProvider { get; }
        
        IReProfilingRequestBuilder ReProfilingRequestBuilder { get; }
        
        IReProfilingResponseMapper ReProfilingResponseMapper { get; }
        
        void AddVariationContext(ProviderVariationContext variationContext);
        
        Task ApplyProviderVariations();

        bool HasVariations { get; }
        
        bool HasErrors { get; }
        
        IEnumerable<string> ErrorMessages { get; }
        
        IEnumerable<PublishedProvider> ProvidersToUpdate { get; }
        
        IEnumerable<PublishedProvider> NewProvidersToAdd { get; }
        
        void AddPublishedProviderToUpdate(PublishedProvider publishedProvider);
        
        void AddNewProviderToAdd(PublishedProvider publishedProvider);
    }
}