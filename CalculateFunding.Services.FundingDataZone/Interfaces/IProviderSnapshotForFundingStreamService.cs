using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.FDZ;

namespace CalculateFunding.Services.FundingDataZone.Interfaces
{
    public interface IProviderSnapshotForFundingStreamService
    {
        Task<IEnumerable<ProviderSnapshot>> GetProviderSnapshotsForFundingStream(string fundingStreamId);
    }
}