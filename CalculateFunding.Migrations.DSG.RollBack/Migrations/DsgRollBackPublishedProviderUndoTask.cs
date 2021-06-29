using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using Serilog;

namespace CalculateFunding.Migrations.DSG.RollBack.Migrations
{
    public class DsgRollBackPublishedProviderUndoTask : PublishedProviderUndoTask
    {
        public DsgRollBackPublishedProviderUndoTask(IPublishedFundingUndoCosmosRepository cosmos,
            IPublishedFundingUndoBlobStoreRepository blobStore,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger,
            IJobTracker jobTracker) 
            : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker, true)
        {
        }

        protected override ICosmosDbFeedIterator GetPublishedProvidersFeed(UndoTaskDetails details)
            => Cosmos.GetPublishedProvidersFromVersion(details.FundingStreamId,
                details.FundingPeriodId,
                details.Version);

        protected override async Task<PublishedProviderVersion> GetPreviousPublishedProviderVersion(string providerId,
            PublishedFundingUndoTaskContext taskContext,
            PublishedProviderStatus? status = null)
        {
            LogInformation($"Querying latest earlier published provider version for '{taskContext.Parameters}'");
            
            UndoTaskDetails taskDetails = taskContext.UndoTaskDetails;

            return await Cosmos.GetLatestEarlierPublishedProviderVersionFromVersion(taskDetails.FundingStreamId,
                taskDetails.FundingPeriodId,
                taskDetails.Version,
                providerId,
                status);
        }
        
        protected override Task NotifyProgress(PublishedFundingUndoTaskContext taskContext) => Task.CompletedTask;
    }
}