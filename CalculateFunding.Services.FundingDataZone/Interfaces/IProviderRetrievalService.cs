using System.Threading.Tasks;
using CalculateFunding.Models.FundingDataZone;

namespace CalculateFunding.Services.FundingDataZone.Interfaces
{
    public interface IProviderRetrievalService
    {
        Task<Provider> GetProviderInSnapshot(int providerSnapshotId, string providerId);
    }
}