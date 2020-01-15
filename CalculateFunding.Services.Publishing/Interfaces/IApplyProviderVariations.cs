using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IApplyProviderVariations
    {
        void AddVariationContext(ProviderVariationContext variationContext);
        Task ApplyProviderVariations();
        bool HasErrors { get; }
        IEnumerable<string> ErrorMessages { get; }
        IEnumerable<PublishedProvider> ProvidersToUpdate { get; }
        IEnumerable<PublishedProvider> NewProvidersToAdd { get; }
        void AddProviderToUpdate(PublishedProvider publishedProvider);
        void AddNewProviderToAdd(PublishedProvider publishedProvider);
    }
}