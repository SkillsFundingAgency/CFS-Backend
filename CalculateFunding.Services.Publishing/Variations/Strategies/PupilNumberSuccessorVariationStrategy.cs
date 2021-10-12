using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<bool> DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));
            
            Provider updatedProvider = providerVariationContext.UpdatedProvider;

            string successorId = updatedProvider.GetSuccessors().SingleOrDefault();

            PublishedProviderVersion priorState = providerVariationContext.PriorState;
            
            if (priorState == null ||
                priorState.Provider.Status == Closed || 
                updatedProvider.Status != Closed ||
                successorId.IsNullOrWhitespace())
            {
                return false;
            }

            PublishedProvider successorProvider = await GetOrCreateSuccessorProvider(providerVariationContext, successorId);

            if (successorProvider == null)
            {
                providerVariationContext.RecordErrors(    
                    $"Unable to run Pupil Number Successor variation as could not locate or create a successor provider with id:{successorId}");

                return false;
            }

            string providerId = providerVariationContext.ProviderId;

            providerVariationContext.Successor = successorProvider;
            
            successorProvider.AddPredecessor(providerId);
            
            providerVariationContext.QueueVariationChange(new MovePupilNumbersToSuccessorChange(providerVariationContext));

            return false;
        }
    }
}
