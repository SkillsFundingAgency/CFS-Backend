using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.FundingDataZone
{
    public interface IPublishingAreaEditorRepository : IPublishingAreaRepository
    {
        Task<IEnumerable<FundingStream>> GetFundingStreams();
        Task<int> GetCountPaymentOrganisationsInSnapshot(int providerSnapshotId, string searchTerm = null);

        Task<int> GetCountProvidersInSnapshot(int providerSnapshotId, string searchTerm = null);

        Task<IEnumerable<ProviderStatus>> GetProviderStatuses();

        Task<FundingStream> CreateFundingStream(FundingStream fundingStream);

        Task<bool> UpdateProvider(PublishingAreaProvider publishingAreaProvider);

        Task<PublishingAreaProvider> InsertProvider(PublishingAreaProvider publishingAreaProvider);

        Task<bool> UpdateOrganisation(PublishingAreaOrganisation publishingAreaOrganisation);

        Task<PublishingAreaOrganisation> InsertOrganisation(PublishingAreaOrganisation organisation);

        Task<bool> UpdateProviderSnapshot(ProviderSnapshotTableModel providerSnapshotTableModel);

        Task<ProviderStatus> CreateProviderStatus(ProviderStatus providerStatus);

        Task CreatePredecessors(IEnumerable<Predecessor> predecessors);

        Task CreateSuccessors(IEnumerable<Successor> successors);

        Task DeletePredecessors(int providerId);

        Task DeleteSuccessors(int providerId);

        Task<int> CloneProviderSnapshot(int providerSnapshotId, string cloneName);

        Task<IEnumerable<PublishingAreaProviderSnapshot>> GetProviderSnapshotsOrderedByTargetDate();

        Task<IEnumerable<PublishingAreaProviderSnapshot>> GetProviderSnapshotsOrderedByTargetDate(int fundingStreamId);

        Task<IEnumerable<PublishingAreaOrganisation>> GetPaymentOrganisationsInSnapshot(int providerSnapshotId, int pageNumber, int pageSize, string searchTerm);

        Task<IEnumerable<ProviderSummary>> GetProvidersInSnapshot(int providerSnapshotId, int pageNumber, int pageSize, string searchTerm);

        Task<PublishingAreaOrganisation> GetOrganisationInSnapshot(int providerSnapshotId, string organisationId);
        Task<ProviderSnapshotTableModel> CreateProviderSnapshot(ProviderSnapshotTableModel providerSnapshot);
    }
}