using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class ClosureWithSuccessorVariationStrategy : Variation, IVariationStrategy
    {
        public string Name => "ClosureWithSuccessor";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));
            
            ApiProvider updatedProvider = providerVariationContext.UpdatedProvider;

            string successorId = updatedProvider.Successor;
            
            if (providerVariationContext.PriorState.Provider.Status == Closed || 
                updatedProvider.Status != Closed ||
                successorId.IsNullOrWhitespace())
            {
                return Task.CompletedTask;
            }
            
            if (providerVariationContext.GeneratedProvider.TotalFunding != providerVariationContext.ReleasedState.TotalFunding)
            {
                providerVariationContext.ErrorMessages.Add("Unable to run Closure with Successor variation as TotalFunding has changed during the refresh funding");
                
                return Task.CompletedTask;
            }

            PublishedProvider successorProvider = providerVariationContext.GetPublishedProviderRefreshState(successorId);

            if (successorProvider == null)
            {
                providerVariationContext.ErrorMessages.Add("Unable to run Closure with Successor variation as could not locate successor provider");
                
                return Task.CompletedTask;
            }

            string providerId = providerVariationContext.ProviderId;
            
            if (successorProvider.HasPredecessor(providerId))
            {
                return Task.CompletedTask;
            }

            providerVariationContext.SuccessorRefreshState = successorProvider.Current;
            
            successorProvider.AddPredecessor(providerId);
            
            providerVariationContext.QueueVariationChange(new TransferRemainingProfilesToSuccessorChange(providerVariationContext));
            providerVariationContext.QueueVariationChange(new ReAdjustSuccessorFundingValuesForProfileValueChange(providerVariationContext));
            providerVariationContext.QueueVariationChange(new ZeroRemainingProfilesChange(providerVariationContext));
            providerVariationContext.QueueVariationChange(new ReAdjustFundingValuesForProfileValuesChange(providerVariationContext));

            return Task.CompletedTask;
        }
    }
}
