using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ISpecificationService
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);
    }
}