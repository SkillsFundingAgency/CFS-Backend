using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;

namespace CalculateFunding.Services.FundingDataZone
{
    public class FundingStreamsWithProviderSnapshotsRetrievalService : IFundingStreamsWithProviderSnapshotsRetrievalService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;

        public FundingStreamsWithProviderSnapshotsRetrievalService(IPublishingAreaRepository publishingAreaRepository)
        {
            _publishingAreaRepository = publishingAreaRepository;
        }

        public async Task<IEnumerable<string>> GetFundingStreamsWithProviderSnapshots()
        {
            return await _publishingAreaRepository.GetFundingStreamsWithProviderSnapshots();
        }
    }
}
