using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Repositories;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Migrations.DSG.RollBack.Migrations
{
    public class DsgRollBackParameters : PublishedFundingUndoJobParameters
    {
        public DsgRollBackParameters(MigrateOptions migrateOptions)
        {
            FundingPeriodId = migrateOptions.FundingPeriod;
            Version = migrateOptions.DocumentVersion;
        }

        public DsgRollBackParameters(Message message) : base(message)
        {
        }
        
        public string FundingPeriodId { get; }
        
        public DocumentVersion Version { get; }

        public static implicit operator DsgRollBackParameters(MigrateOptions options)
        {
            return new DsgRollBackParameters(options);
        }

        public override string ToString() => $"FundingPeriodId: {FundingPeriodId}, Version: {Version}";
    }
}