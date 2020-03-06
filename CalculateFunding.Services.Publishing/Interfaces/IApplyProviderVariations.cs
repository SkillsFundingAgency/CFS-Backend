using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IApplyProviderVariations
    {
        IPublishingResiliencePolicies ResiliencePolicies { get; }
        
        ISpecificationsApiClient SpecificationsApiClient { get; }
        
        void AddVariationContext(ProviderVariationContext variationContext);
        
        Task ApplyProviderVariations();
        
        bool HasErrors { get; }
        
        IEnumerable<string> ErrorMessages { get; }
        
        IEnumerable<PublishedProvider> ProvidersToUpdate { get; }
        
        IEnumerable<PublishedProvider> NewProvidersToAdd { get; }
        
        void AddPublishedProviderToUpdate(PublishedProvider publishedProvider);
        
        void AddNewProviderToAdd(PublishedProvider publishedProvider);
    }
}