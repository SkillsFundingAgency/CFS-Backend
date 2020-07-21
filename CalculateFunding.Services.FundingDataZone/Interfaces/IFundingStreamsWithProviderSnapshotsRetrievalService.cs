using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FundingDataZone.Interfaces
{
    public interface IFundingStreamsWithProviderSnapshotsRetrievalService
    {
        Task<IEnumerable<string>> GetFundingStreamsWithProviderSnapshots();
    }
}