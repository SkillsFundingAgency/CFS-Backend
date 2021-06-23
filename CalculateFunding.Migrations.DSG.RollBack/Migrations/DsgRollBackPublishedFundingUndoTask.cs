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
    public class DsgRollBackPublishedFundingUndoTask : PublishedFundingUndoTask
    {
        public DsgRollBackPublishedFundingUndoTask(IPublishedFundingUndoCosmosRepository cosmos,
            IPublishedFundingUndoBlobStoreRepository blobStore,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger,
            IJobTracker jobTracker) 
            : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker, true)
        {
        }

        protected override ICosmosDbFeedIterator GetPublishedFundingFeed(UndoTaskDetails details)
            => Cosmos.GetPublishedFundingFromVersion(details.FundingStreamId,
                details.FundingPeriodId,
                details.Version);

        protected override async Task<PublishedFundingVersion> GetPreviousPublishedFundingVersion(PublishedFundingVersion currentVersion,
            PublishedFundingUndoTaskContext taskContext)
        {
            LogInformation($"Querying latest earlier published funding version for '{taskContext.Parameters}'");
            
            UndoTaskDetails details = taskContext.PublishedFundingVersionDetails;

            return await Cosmos.GetLatestEarlierPublishedFundingVersionFromVersion(details.FundingStreamId,
                details.FundingPeriodId,
                details.Version,
                currentVersion.OrganisationGroupTypeIdentifier,
                currentVersion.OrganisationGroupIdentifierValue,
                currentVersion.GroupingReason);
        }

        protected override Task NotifyProgress(PublishedFundingUndoTaskContext taskContext) => Task.CompletedTask;
    }
}