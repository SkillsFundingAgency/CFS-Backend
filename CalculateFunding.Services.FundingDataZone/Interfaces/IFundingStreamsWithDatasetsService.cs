using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FundingDataZone.Interfaces
{
    public interface IFundingStreamsWithDatasetsService
    {
        Task<IEnumerable<string>> GetFundingStreamsWithDatasets();
    }
}