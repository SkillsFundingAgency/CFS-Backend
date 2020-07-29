using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using Serilog;

namespace CalculateFunding.Migrations.DSG.RollBack.Migrations
{
    public class DsgRollBackInitialisationTask
        : UndoTaskBase, IPublishedFundingUndoJobTask
    {
        public DsgRollBackInitialisationTask(IPublishedFundingUndoCosmosRepository cosmos, 
            IPublishedFundingUndoBlobStoreRepository blobStore,
            IProducerConsumerFactory producerConsumerFactory,
            ILogger logger,
            IJobTracker jobTracker) 
            : base(cosmos, blobStore, producerConsumerFactory, logger, jobTracker)
        {
        }

        public Task Run(PublishedFundingUndoTaskContext taskContext)
        {
            DsgRollBackParameters parameters = taskContext.Parameters as DsgRollBackParameters;
            
            Guard.ArgumentNotNull(parameters, nameof(parameters));
            
            taskContext.PublishedFundingDetails =
                taskContext.PublishedProviderDetails =
                    taskContext.PublishedProviderVersionDetails =
                        taskContext.PublishedFundingVersionDetails = new UndoTaskDetails
                        {
                            FundingPeriodId = parameters.FundingPeriodId,
                            FundingStreamId = "DSG",
                            Version = parameters.Version.DecimalValue
                        };
            
            return Task.CompletedTask;
        }
    }
}