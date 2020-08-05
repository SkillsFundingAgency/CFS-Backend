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
    public class OrganisationsRetrievalService : IOrganisationsRetrievalService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;
        private readonly IMapper _mapper;

        public OrganisationsRetrievalService(IPublishingAreaRepository publishingAreaRepository,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(publishingAreaRepository, nameof(publishingAreaRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            
            _publishingAreaRepository = publishingAreaRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PaymentOrganisation>> GetAllOrganisations(int providerSnapshotId)
        {
            IEnumerable<PublishingAreaOrganisation> organisations = await _publishingAreaRepository.GetAllOrganisations(providerSnapshotId);

            return organisations.Select(MapProviderOrganisation).Where(_ => _ != null).ToArray();
        }

        private PaymentOrganisation MapProviderOrganisation(PublishingAreaOrganisation snapshot) => _mapper.Map<PaymentOrganisation>(snapshot);
    }
}
