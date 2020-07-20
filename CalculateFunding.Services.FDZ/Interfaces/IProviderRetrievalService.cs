using CalculateFunding.Models.FDZ;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FDZ.Interfaces
{
    public interface IProviderRetrievalService
    {
        Task<Provider> GetProviderInSnapshot(int providerSnapshotId, string providerId);
    }
}