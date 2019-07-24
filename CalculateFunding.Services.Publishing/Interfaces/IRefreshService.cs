using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IRefreshService
    {
        Task RefreshResults(Message message);
        
        Task<ApiSpecificationSummary> GetSpecificationSummaryById(string specificationId);
    }
}
