using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.FDZ.Interfaces
{
    public interface IFundingStreamsWithDatasetsService
    {
        Task<IEnumerable<string>> GetFundingStreamsWithDatasets();
    }
}