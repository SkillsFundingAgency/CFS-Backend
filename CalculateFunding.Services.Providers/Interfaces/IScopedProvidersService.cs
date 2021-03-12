using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IScopedProvidersService : IJobProcessingService
    {
        Task<IActionResult> FetchCoreProviderData(string specificationId, string providerVersionId = null);
        Task<IActionResult> PopulateProviderSummariesForSpecification(string specificationId, string correlationId, Reference user, bool setCachedProviders = false);
        Task<IActionResult> GetScopedProviderIds(string specificationId);
    }
}
