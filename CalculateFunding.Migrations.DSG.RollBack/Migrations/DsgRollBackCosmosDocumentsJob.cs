using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using Serilog;

namespace CalculateFunding.Migrations.DSG.RollBack.Migrations
{
    public class DsgRollBackCosmosDocumentsJob : PublishedFundingUndoJobService, IDsgRollBackCosmosDocumentsJob
    {
        public DsgRollBackCosmosDocumentsJob(IPublishedFundingUndoTaskFactoryLocator factoryLocator,
            IJobManagement jobManagement,
            IPublishedFundingUndoJobCreation jobCreation,
            ILogger logger) : base(factoryLocator, jobManagement, jobCreation, logger)
        {
        }

        public async Task Run(MigrateOptions migrateOptions)
        {
            DsgRollBackParameters rollBackParameters = migrateOptions;

            await PerformUndo(rollBackParameters);
        }
    }
}