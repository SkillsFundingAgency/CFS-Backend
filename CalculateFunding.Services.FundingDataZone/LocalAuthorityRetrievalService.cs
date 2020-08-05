using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone
{
    public class LocalAuthorityRetrievalService : ILocalAuthorityRetrievalService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;
        private readonly IMapper _mapper;

        public LocalAuthorityRetrievalService(IPublishingAreaRepository publishingAreaRepository,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(publishingAreaRepository, nameof(publishingAreaRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            
            _publishingAreaRepository = publishingAreaRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PaymentOrganisation>> GetLocalAuthorities(int providerSnapshotId)
        {
            IEnumerable<PublishingAreaOrganisation> localAuthorities = await _publishingAreaRepository.GetLocalAuthorities(providerSnapshotId);

            return localAuthorities?.Select(MapProviderOrganisation).Where(_ => _ != null).ToArray() ?? ArraySegment<PaymentOrganisation>.Empty;
        }

        private PaymentOrganisation MapProviderOrganisation(PublishingAreaOrganisation snapshot) => _mapper.Map<PaymentOrganisation>(snapshot);
    }
}
