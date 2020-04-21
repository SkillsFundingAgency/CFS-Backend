using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class ClosureWithSuccessorVariationStrategy : Variation, IVariationStrategy
    {
        private readonly IProviderService _providerService;

        public ClosureWithSuccessorVariationStrategy(IProviderService providerService)
        {
            Guard.ArgumentNotNull(providerService, nameof(providerService));

            _providerService = providerService;
        }
        
        public string Name => "ClosureWithSuccessor";

        public async Task DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));
            
            Provider updatedProvider = providerVariationContext.UpdatedProvider;

            string successorId = updatedProvider.Successor;
            
            if (providerVariationContext.PriorState.Provider.Status == Closed || 
                updatedProvider.Status != Closed ||
                successorId.IsNullOrWhitespace())
            {
                return;
            }
            
            if (providerVariationContext.UpdatedTotalFunding != providerVariationContext.PriorState.TotalFunding)
            {
                providerVariationContext.ErrorMessages.Add("Unable to run Closure with Successor variation as TotalFunding has changed during the refresh funding");

                return;
            }

            PublishedProvider successorProvider = await GetOrCreateSuccessorProvider(providerVariationContext, successorId);

            if (successorProvider == null)
            {
                providerVariationContext.ErrorMessages.Add($"Unable to run Closure with Successor variation as could not locate or create a successor provider with id:{successorId}");

                return;
            }

            string providerId = providerVariationContext.ProviderId;
            
            if (successorProvider.HasPredecessor(providerId))
            {
                return;
            }

            providerVariationContext.SuccessorRefreshState = successorProvider.Current;
            
            successorProvider.AddPredecessor(providerId);
            
            providerVariationContext.QueueVariationChange(new TransferRemainingProfilesToSuccessorChange(providerVariationContext));
            providerVariationContext.QueueVariationChange(new ReAdjustSuccessorFundingValuesForProfileValueChange(providerVariationContext));
            providerVariationContext.QueueVariationChange(new ZeroRemainingProfilesChange(providerVariationContext));
            providerVariationContext.QueueVariationChange(new ReAdjustFundingValuesForProfileValuesChange(providerVariationContext));

            return;
        }

        private async Task<PublishedProvider> GetOrCreateSuccessorProvider(ProviderVariationContext providerVariationContext,
            string successorId)
        {
            return providerVariationContext.GetPublishedProviderRefreshState(successorId) ??  
                providerVariationContext.AddMissingProvider(await _providerService.CreateMissingPublishedProviderForPredecessor(providerVariationContext.PublishedProvider,
                                                                                                                            successorId, providerVariationContext.ProviderVersionId));
        }
    }
}
