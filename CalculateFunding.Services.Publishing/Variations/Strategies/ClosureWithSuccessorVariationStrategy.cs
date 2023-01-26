using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Serilog;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class ClosureWithSuccessorVariationStrategy : SuccessorVariationStrategy, IVariationStrategy
    {
        private string _successorId;
        private readonly ILogger _logger;

        public ClosureWithSuccessorVariationStrategy(IProviderService providerService, ILogger logger) 
            : base(providerService)
        {
            _logger = logger;
        }
        
        public override string Name => "ClosureWithSuccessor";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));
            
            Provider updatedProvider = providerVariationContext.UpdatedProvider;

            PublishedProviderVersion priorState = providerVariationContext.PriorState;
            //Adding logs to get the successors
            _logger.Information("Getting the successor for provider '{ProviderId}'", updatedProvider?.ProviderId);
            var successorList = updatedProvider?.GetSuccessors();
            if (successorList.Any())
            {
                foreach (var successor in successorList)
                {
                    _logger.Information("List of the successors '{SuccessorList}'", successor);
                }
            }
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
            if (providerVariationContext.UpdatedTotalFunding != providerVariationContext.PriorState.TotalFunding)
            {
                RecordError(providerVariationContext,
                    $"Unable to run Closure with Successor variation as TotalFunding has changed during the refresh funding for provider with id:{providerVariationContext.ProviderId}");

                return false;
            }

            PublishedProvider successor = await GetOrCreateSuccessorProvider(providerVariationContext, _successorId);

            if (successor == null)
            {
                RecordError(providerVariationContext,
                    $"Could not locate or create a successor provider with id:{_successorId}");

                return false;
            }

            if (successor.HasPredecessor(providerVariationContext.ProviderId))
            {
                return false;
            }

            providerVariationContext.Successor = successor;

            successor.AddPredecessor(providerVariationContext.ProviderId);

            providerVariationContext.QueueVariationChange(new TransferRemainingProfilesToSuccessorChange(providerVariationContext, Name));
            providerVariationContext.QueueVariationChange(new ReAdjustSuccessorFundingValuesForProfileValueChange(providerVariationContext, Name));
            providerVariationContext.QueueVariationChange(new ZeroRemainingProfilesChange(providerVariationContext, Name));
            providerVariationContext.QueueVariationChange(new ReAdjustFundingValuesForProfileValuesChange(providerVariationContext, Name));

            return false;
        }
    }
}
