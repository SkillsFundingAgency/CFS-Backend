using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IResultsRepository
    {
        Task<IEnumerable<string>> GetAllProviderIdsForSpecificationId(string specificationId);
    }
}
