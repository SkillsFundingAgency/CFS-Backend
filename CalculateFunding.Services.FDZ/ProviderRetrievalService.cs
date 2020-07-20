﻿using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.FDZ;
using CalculateFunding.Services.FDZ.Interfaces;
using CalculateFunding.Services.FDZ.SqlModels;

namespace CalculateFunding.Services.FDZ
{
    public class ProviderRetrievalService : IProviderRetrievalService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;
        private readonly IMapper _mapper;

        public ProviderRetrievalService(
            IPublishingAreaRepository publishingAreaRepository,
            IMapper mapper)
        {
            _publishingAreaRepository = publishingAreaRepository;
            _mapper = mapper;
        }

        public async Task<Provider> GetProviderInSnapshot(int providerSnapshotId, string providerId)
        {
            PublishingAreaProvider provider = await _publishingAreaRepository.GetProviderInSnapshot(providerSnapshotId, providerId);

            return _mapper.Map<Provider>(provider);
        }
    }
}
