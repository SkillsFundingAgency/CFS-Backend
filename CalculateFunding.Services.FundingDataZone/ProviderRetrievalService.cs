using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone
{
    public class ProviderRetrievalService : IProviderRetrievalService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMapper _mapper;

        public ProviderRetrievalService(
            IPublishingAreaRepository publishingAreaRepository,
            ICacheProvider cacheProvider,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(publishingAreaRepository, nameof(publishingAreaRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            
            _publishingAreaRepository = publishingAreaRepository;
            _mapper = mapper;
            _cacheProvider = cacheProvider;
        }

        public async Task<Provider> GetProviderInSnapshot(int providerSnapshotId, string providerId)
        {
            PublishingAreaProvider provider = await _publishingAreaRepository.GetProviderInSnapshot(providerSnapshotId, providerId);

            return _mapper.Map<Provider>(provider);
        }

        public async Task DisableTrackLatest(bool disableToggleTracking)
        {
            await _cacheProvider.SetAsync(CacheKeys.DisableTrackLatest, disableToggleTracking);
        }
        public async Task<bool> GetDisableTrackLatest()
        {
            return await _cacheProvider.GetAsync<bool>(CacheKeys.DisableTrackLatest);
        }
    }
}
