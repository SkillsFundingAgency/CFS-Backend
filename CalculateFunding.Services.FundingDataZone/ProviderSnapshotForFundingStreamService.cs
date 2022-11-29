using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone
{
    public class ProviderSnapshotForFundingStreamService : IProviderSnapshotForFundingStreamService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;
        private readonly IMapper _mapper;

        public ProviderSnapshotForFundingStreamService(IPublishingAreaRepository publishingAreaRepository,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(publishingAreaRepository, nameof(publishingAreaRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _publishingAreaRepository = publishingAreaRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProviderSnapshot>> GetLatestProviderSnapshotsForAllFundingStreams()
        {
            IEnumerable<PublishingAreaProviderSnapshot> providerSnapshots = await _publishingAreaRepository.GetLatestProviderSnapshotsForAllFundingStreams();

            return _mapper.Map<IEnumerable<ProviderSnapshot>>(providerSnapshots);
        }
        public async Task<IEnumerable<ProviderSnapshot>> GetProviderSnapshotsForFundingStream(string fundingStreamId ,string fundingPeriodId)
        {
            IEnumerable<PublishingAreaProviderSnapshot> providerSnapshots = await _publishingAreaRepository.GetProviderSnapshots(fundingStreamId, fundingPeriodId);

            return _mapper.Map<IEnumerable<ProviderSnapshot>>(providerSnapshots);
        }

        public async Task<IEnumerable<ProviderSnapshot>> GetLatestProviderSnapshotsForAllFundingStreamsWithFundingPeriod()
        {
            IEnumerable<PublishingAreaProviderSnapshot> providerSnapshots = await _publishingAreaRepository.GetLatestProviderSnapshotsForAllFundingStreamsWithFundingPeriod();

            return _mapper.Map<IEnumerable<ProviderSnapshot>>(providerSnapshots);
        }

    }
}