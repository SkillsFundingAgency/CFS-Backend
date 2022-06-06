using CalculateFunding.Common.Utility;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FundingDataZone
{
    public class ProviderSnapshotFundingPeriodService : IProviderSnapshotFundingPeriodService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;
        public ProviderSnapshotFundingPeriodService(IPublishingAreaRepository publishingAreaRepository)
        {
            Guard.ArgumentNotNull(publishingAreaRepository, nameof(publishingAreaRepository));

            _publishingAreaRepository = publishingAreaRepository;
        }

        public async Task PopulateFundingPeriod(int providerSnapshotId)
        {
            await _publishingAreaRepository.PopulateFundingPeriod(providerSnapshotId);
        }

        public async Task PopulateFundingPeriods()
        {
            await _publishingAreaRepository.PopulateFundingPeriods();
        }
    }
}
