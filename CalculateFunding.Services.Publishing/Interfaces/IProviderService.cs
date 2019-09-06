using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Providers.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProviderService
    {
        Task<IEnumerable<Provider>> GetProvidersByProviderVersionsId(string providerVersionId);
        Task<IEnumerable<Provider>> GetScopedProvidersForSpecification(string specificationId, string providerVersionId);
    }
}