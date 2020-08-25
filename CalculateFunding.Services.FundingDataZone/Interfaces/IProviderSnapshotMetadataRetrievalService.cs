using System.Threading.Tasks;
using CalculateFunding.Models.FundingDataZone;

namespace CalculateFunding.Services.FundingDataZone.Interfaces
{
    public interface IProviderSnapshotMetadataRetrievalService
    {
        Task<ProviderSnapshot> GetProviderSnapshotsMetadata(int providerSnapshotId);
    }
}