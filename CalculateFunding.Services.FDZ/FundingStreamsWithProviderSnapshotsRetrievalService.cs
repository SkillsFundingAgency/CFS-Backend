using CalculateFunding.Services.FDZ.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FDZ
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
