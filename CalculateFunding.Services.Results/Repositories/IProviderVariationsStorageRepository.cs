using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Repositories
{
    public interface IProviderVariationsStorageRepository
    {
        Task<string> SaveErrors(string specificationId, string jobId, IEnumerable<ProviderVariationError> errors);
    }
}