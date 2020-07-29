using System.Threading.Tasks;

namespace CalculateFunding.Migrations.DSG.RollBack.Migrations
{
    public interface IDsgRollBackCosmosDocumentsJob
    {
        Task Run(MigrateOptions migrateOptions);
    }
}