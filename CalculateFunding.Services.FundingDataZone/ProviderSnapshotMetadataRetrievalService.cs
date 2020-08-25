using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone
{
    public class ProviderSnapshotMetadataRetrievalService : IProviderSnapshotMetadataRetrievalService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;
        private readonly IMapper _mapper;

        public ProviderSnapshotMetadataRetrievalService(IPublishingAreaRepository publishingAreaRepository,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(publishingAreaRepository, nameof(publishingAreaRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _publishingAreaRepository = publishingAreaRepository;
            _mapper = mapper;
        }

        public async Task<ProviderSnapshot> GetProviderSnapshotsMetadata(int providerSnapshotId)
        {
            PublishingAreaProviderSnapshot providerSnapshotMetadata = await _publishingAreaRepository.GetProviderSnapshotMetadata(providerSnapshotId);

            return _mapper.Map<ProviderSnapshot>(providerSnapshotMetadata);
        }
    }
}