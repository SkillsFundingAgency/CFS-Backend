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
        private string _successorId;

        public PupilNumberSuccessorVariationStrategy(IProviderService providerService) 
            : base(providerService)
        {
        }

        public override string Name => "PupilNumberSuccessor";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Provider updatedProvider = providerVariationContext.UpdatedProvider;

            PublishedProviderVersion priorState = providerVariationContext.PriorState;

            _successorId = updatedProvider.GetSuccessors().SingleOrDefault();

            if (priorState == null ||
                ShouldSkipIfClosed(priorState.Provider) ||
                updatedProvider.Status != Closed ||
                _successorId.IsNullOrWhitespace())
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private bool ShouldSkipIfClosed(Provider provider)
        {
            if (provider.Status == Closed && provider.ReasonEstablishmentClosed == AcademyConverter)
            {
                return provider.GetSuccessors().AnyWithNullCheck();
            }

            return provider.Status == Closed;
        }

        protected override async Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            PublishedProvider successor = await GetOrCreateSuccessorProvider(providerVariationContext, _successorId);

            if (successor == null)
            {
                RecordError(providerVariationContext,
                    $"Could not locate or create a successor provider with id:{_successorId}");

                return false;
            }

            providerVariationContext.Successor = successor;

            successor.AddPredecessor(providerVariationContext.ProviderId);

            providerVariationContext.QueueVariationChange(new MovePupilNumbersToSuccessorChange(providerVariationContext, Name));

            return false;
        }
    }
}
