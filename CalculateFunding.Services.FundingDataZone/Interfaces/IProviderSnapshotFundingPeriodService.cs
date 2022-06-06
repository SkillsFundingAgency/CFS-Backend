using System.Threading.Tasks;

namespace CalculateFunding.Services.FundingDataZone.Interfaces
{
    public interface IProviderSnapshotFundingPeriodService
    {
        Task PopulateFundingPeriods();

        Task PopulateFundingPeriod(int providerSnapshotId);
    }
}
