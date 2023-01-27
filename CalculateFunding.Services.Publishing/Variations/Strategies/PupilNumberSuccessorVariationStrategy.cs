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
    public class PupilNumberSuccessorVariationStrategy : SuccessorVariationStrategy, IVariationStrategy
    {
        private string _successorId;
        ILogger _logger;

        public PupilNumberSuccessorVariationStrategy(IProviderService providerService, ILogger logger) 
            : base(providerService)
        {
            _logger = logger;
        }

        public override string Name => "PupilNumberSuccessor";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Provider updatedProvider = providerVariationContext.UpdatedProvider;

            PublishedProviderVersion priorState = providerVariationContext.PriorState;

            //Adding logs to get the successors
            _logger.Information("Getting the successor for provider '{ProviderId}'", updatedProvider?.ProviderId);
            var successorList = updatedProvider?.GetSuccessors();
            if (successorList.Any() && successorList.Count() > 1)
            {
                foreach (var successor in successorList)
                {
                    _logger.Information("List of the successors '{SuccessorList}'", successor);
                }
            }
            //Changing the logic to FirstorDefault from SingleorDefault for PSG issue

            //_successorId = updatedProvider.GetSuccessors().SingleOrDefault();

            _successorId = updatedProvider.GetSuccessors().FirstOrDefault();

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
