using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone
{
    public class ProvidersInSnapshotRetrievalService : IProvidersInSnapshotRetrievalService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;
        private readonly IMapper _mapper;

        public ProvidersInSnapshotRetrievalService(
            IPublishingAreaRepository publishingAreaRepository,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(publishingAreaRepository, nameof(publishingAreaRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            
            _publishingAreaRepository = publishingAreaRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Provider>> GetProvidersInSnapshot(int providerSnapshotId)
        {
            IEnumerable<PublishingAreaProvider> providers = await _publishingAreaRepository.GetProvidersInSnapshot(providerSnapshotId);

            return _mapper.Map<IEnumerable<Provider>>(providers);
        }
    }
}
