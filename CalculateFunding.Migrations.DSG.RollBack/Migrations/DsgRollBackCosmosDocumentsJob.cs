using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using Serilog;

namespace CalculateFunding.Migrations.DSG.RollBack.Migrations
{
    public class DsgRollBackCosmosDocumentsJob : PublishedFundingUndoJobService, IDsgRollBackCosmosDocumentsJob
    {
        public DsgRollBackCosmosDocumentsJob(IPublishedFundingUndoTaskFactoryLocator factoryLocator,
            IJobTracker jobTracker,
            IPublishedFundingUndoJobCreation jobCreation,
            ILogger logger) : base(factoryLocator, jobTracker, jobCreation, logger)
        {
        }

        public async Task Run(MigrateOptions migrateOptions)
        {
            DsgRollBackParameters rollBackParameters = migrateOptions;

            await PerformUndo(rollBackParameters);
        }

        protected override Task FailJob(string message,
            PublishedFundingUndoJobParameters parameters) =>
            Task.CompletedTask;

        protected override Task CompleteTrackingJob(PublishedFundingUndoJobParameters parameters) => Task.CompletedTask;

        protected override Task<bool> TryStartTrackingJob(PublishedFundingUndoJobParameters parameters) => Task.FromResult(true);
    }
}