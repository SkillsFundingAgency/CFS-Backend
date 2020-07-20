using CalculateFunding.Models.FDZ;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FDZ.Interfaces
{
    public interface IProvidersInSnapshotRetrievalService
    {
        Task<IEnumerable<Provider>> GetProvidersInSnapshot(int providerSnapshotId);
    }
}