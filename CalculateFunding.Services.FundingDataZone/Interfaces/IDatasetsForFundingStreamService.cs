using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.FundingDataZone;

namespace CalculateFunding.Services.FundingDataZone.Interfaces
{
    public interface IDatasetsForFundingStreamService
    {
        Task<IEnumerable<Dataset>> GetDatasetsForFundingStream(string fundingStreamId);
    }
}