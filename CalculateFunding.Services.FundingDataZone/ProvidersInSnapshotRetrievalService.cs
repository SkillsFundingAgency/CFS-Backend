using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.FDZ;
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
            _publishingAreaRepository = publishingAreaRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Provider>> GetProvidersInSnapshot(int providerSnapshotId)
        {
            IEnumerable<PublishingAreaProvider> providers = await _publishingAreaRepository.GetProvidersInSnapshot(providerSnapshotId);

            List<Provider> results = new List<Provider>(providers.Count());

            foreach (PublishingAreaProvider provider in providers)
            {
                results.Add(_mapper.Map<Provider>(provider));
            }

            return results;
        }
    }
}
