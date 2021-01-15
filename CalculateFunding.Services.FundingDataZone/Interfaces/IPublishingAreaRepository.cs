using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone.Interfaces
{
    public interface IPublishingAreaRepository
    {
        Task<IEnumerable<PublishingAreaDatasetMetadata>> GetDatasetMetadata(string fundingStreamId);
        Task<IEnumerable<PublishingAreaProviderSnapshot>> GetProviderSnapshots(string fundingStreamId);
        Task<string> GetTableNameForDataset(string datasetCode, int version);
        Task<object> GetDataForTable(string tableName);
        Task<IEnumerable<PublishingAreaProvider>> GetProvidersInSnapshot(int providerSnapshotId);
        Task<PublishingAreaProvider> GetProviderInSnapshot(int providerSnapshotId, string providerId);
        Task<IEnumerable<string>> GetFundingStreamsWithProviderSnapshots();
        Task<IEnumerable<PublishingAreaOrganisation>> GetAllOrganisations(int providerSnapshotId);
        Task<IEnumerable<string>> GetFundingStreamsWithDatasets();
        Task<IEnumerable<PublishingAreaOrganisation>> GetLocalAuthorities(int providerSnapshotId);
        Task<PublishingAreaProviderSnapshot> GetProviderSnapshotMetadata(int providerSnapshotId);
        Task<IEnumerable<PublishingAreaProviderSnapshot>> GetLatestProviderSnapshotsForAllFundingStreams();
    }
}