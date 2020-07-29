using System.Threading.Tasks;

namespace CalculateFunding.Migrations.DSG.RollBack.Migrations
{
    public interface IRollBackDsg
    {
        Task Run(MigrateOptions options);
    }
}