using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.FundingDataZone;

namespace CalculateFunding.Services.FundingDataZone.Interfaces
{
    public interface IProviderSnapshotForFundingStreamService
    {
        Task<IEnumerable<ProviderSnapshot>> GetProviderSnapshotsForFundingStream(string fundingStreamId, string fundingPeriodId);
        Task<IEnumerable<ProviderSnapshot>> GetLatestProviderSnapshotsForAllFundingStreams();
        Task<IEnumerable<ProviderSnapshot>> GetLatestProviderSnapshotsForAllFundingStreamsWithFundingPeriod();
        
    }
}