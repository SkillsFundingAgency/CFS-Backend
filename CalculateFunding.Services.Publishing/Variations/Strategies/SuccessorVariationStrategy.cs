using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public abstract class SuccessorVariationStrategy : VariationStrategy
    {
        private readonly IProviderService _providerService;

        protected SuccessorVariationStrategy(IProviderService providerService)
        {
            Guard.ArgumentNotNull(providerService, nameof(providerService));

            _providerService = providerService;
        }

        protected async Task<PublishedProvider> GetOrCreateSuccessorProvider(ProviderVariationContext providerVariationContext,
            string successorId)
        {
            return providerVariationContext.GetPublishedProviderRefreshState(successorId) ??  
                   providerVariationContext.AddMissingProvider(await _providerService.CreateMissingPublishedProviderForPredecessor(
                       providerVariationContext.PublishedProvider,
                       successorId, providerVariationContext.ProviderVersionId));
        }
    }
}