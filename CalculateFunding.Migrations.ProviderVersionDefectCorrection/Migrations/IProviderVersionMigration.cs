using System.Threading.Tasks;

namespace CalculateFunding.Migrations.ProviderVersionDefectCorrection.Migrations
{
    public interface IProviderVersionMigration
    {
        Task Run();
    }
}