﻿using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone
{
    public class LocalAuthorityRetrievalService : ILocalAuthorityRetrievalService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;

        public LocalAuthorityRetrievalService(IPublishingAreaRepository publishingAreaRepository)
        {
            _publishingAreaRepository = publishingAreaRepository;
        }

        public async Task<IEnumerable<PaymentOrganisation>> GetLocalAuthorities(int providerSnapshotId)
        {
            IEnumerable<PublishingAreaOrganisation> sqlResults = await _publishingAreaRepository.GetLocalAuthorities(providerSnapshotId);

            List<PaymentOrganisation> results = new List<PaymentOrganisation>();
            foreach (var snapshot in sqlResults)
            {
                PaymentOrganisation providerSnapshot = MapProviderOrganisation(snapshot);
                if (providerSnapshot != null)
                {
                    results.Add(providerSnapshot);
                }
            }

            return results;
        }

        private PaymentOrganisation MapProviderOrganisation(PublishingAreaOrganisation snapshot)
        {
            return new PaymentOrganisation()
            {
                ProviderSnapshotId = snapshot.ProviderSnapshotId,
                CompanyHouseNumber = snapshot.CompanyHouseNumber,
                LaCode = snapshot.LaCode,
                Name = snapshot.LaCode,
                PaymentOrganisationId = snapshot.PaymentOrganisationId,
                OrganisationType = snapshot.PaymentOrganisationType,
                TrustCode = snapshot.TrustCode,
                Ukprn = snapshot.Ukprn,
                Upin = snapshot.Upin,
                Urn = snapshot.Urn,
            };
        }
    }
}
