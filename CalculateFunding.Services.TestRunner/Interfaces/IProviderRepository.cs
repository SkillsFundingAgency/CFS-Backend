using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IProviderRepository
    {
        Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsBySpecificationId(string specificationId);
    }
}   

