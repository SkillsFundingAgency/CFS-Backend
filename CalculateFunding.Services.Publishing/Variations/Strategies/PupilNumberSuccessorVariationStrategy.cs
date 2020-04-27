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
    public class PupilNumberSuccessorVariationStrategy : SuccessorVariationStrategy, IVariationStrategy
    {
        public PupilNumberSuccessorVariationStrategy(IProviderService providerService) 
            : base(providerService)
        {
        }

        public string Name => "PupilNumberSuccessor";

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

            PublishedProvider successorProvider = await GetOrCreateSuccessorProvider(providerVariationContext, successorId);

            if (successorProvider == null)
            {
                providerVariationContext.RecordErrors(    
                    $"Unable to run Pupil Number Successor variation as could not locate or create a successor provider with id:{successorId}");

                return;
            }

            string providerId = providerVariationContext.ProviderId;

            providerVariationContext.SuccessorRefreshState = successorProvider.Current;
            
            successorProvider.AddPredecessor(providerId);
            
            providerVariationContext.QueueVariationChange(new MovePupilNumbersToSuccessorChange(providerVariationContext));
        }
    }
}
