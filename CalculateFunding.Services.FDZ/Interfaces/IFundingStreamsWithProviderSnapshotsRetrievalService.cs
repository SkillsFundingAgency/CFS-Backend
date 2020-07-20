using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FDZ.Interfaces
{
    public interface IFundingStreamsWithProviderSnapshotsRetrievalService
    {
        Task<IEnumerable<string>> GetFundingStreamsWithProviderSnapshots();
    }
}