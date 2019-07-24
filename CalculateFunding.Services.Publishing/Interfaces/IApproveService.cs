using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IApproveService
    {
        Task ApproveResults(Message message);
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);
    }
}
