using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Providers.Models;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IRefreshService
    {
        Task RefreshResults(Message message);
        
        Task<ApiSpecificationSummary> GetSpecificationSummaryById(string specificationId);
        Task<IEnumerable<Provider>> GetProvidersByProviderVersionId(string providerVersionId);
    }
}
