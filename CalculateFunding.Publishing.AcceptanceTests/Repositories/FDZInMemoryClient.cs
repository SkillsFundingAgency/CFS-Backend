using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class FDZInMemoryClient : IFundingDataZoneApiClient
    {
        private ConcurrentDictionary<int, ConcurrentBag<PaymentOrganisation>> _organisationGroups = new();

        public Task<ApiResponse<IEnumerable<PaymentOrganisation>>> GetAllOrganisations(int providerSnapshotId)
        {
            if (_organisationGroups.TryGetValue(providerSnapshotId, out ConcurrentBag<PaymentOrganisation> paymentOrganisations))
            {
                return Task.FromResult(new ApiResponse<IEnumerable<PaymentOrganisation>>(System.Net.HttpStatusCode.OK, paymentOrganisations));
            }
            else
            {
                throw new InvalidOperationException("Unable to find provider snapshot");
            }
        }

        public void SetPaymentOrganisationsForProviderSnapshot(int providerSnapshotId, IEnumerable<PaymentOrganisation> paymentOrganisations)
        {
            ConcurrentBag<PaymentOrganisation> organisationGroups = _organisationGroups.GetOrAdd(providerSnapshotId, (existing) => { return new ConcurrentBag<PaymentOrganisation>(); });
           
            paymentOrganisations.ForEach(_ => organisationGroups.Add(_));
        }

        public Task<ApiResponse<object>> GetDataForDatasetVersion(string datasetCode, int versionNumber)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<DatasetMetadata>>> GetDatasetMetadataForDataset(string fundingStreamId, string datasetCode, int versionNumber)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<Dataset>>> GetDatasetsAndVersionsForFundingStream(string fundingStreamId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<DatasetMetadata>>> GetDatasetsForFundingStream(string fundingStreamId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<DatasetMetadata>>> GetDatasetVersionsForDataset(string fundingStreamId, string datasetCode)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<string>>> GetFundingStreamsWithDatasets()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<ProviderSnapshot>>> GetLatestProviderSnapshotsForAllFundingStreams()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<PaymentOrganisation>>> GetLocalAuthorities(int providerSnapshotId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<Provider>>> GetProvidersInSnapshot(int providerSnapshotId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<Provider>> GetProviderInSnapshot(int providerSnapshotId, string providerId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<Provider>>> GetProviderSnapshotMetadata(int providerSnapshotId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<ProviderSnapshot>>> GetProviderSnapshotsForFundingStream(string fundingStreamId,string fundingPeriodId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<string>>> ListFundingStreamsWithProviderSnapshots()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<ProviderSnapshot>>> GetLatestProviderSnapshotsForAllFundingStreamsWithFundingPeriod()
        {
            throw new NotImplementedException();
        }
    }
}
