using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IScopedProvidersService : IJobProcessingService
    {
        Task<IActionResult> FetchCoreProviderData(string specificationId);
        Task<IActionResult> PopulateProviderSummariesForSpecification(string specificationId, string correlationId, Reference user, bool setCachedProviders = false);
        Task<IActionResult> GetScopedProviderIds(string specificationId);
    }
}
