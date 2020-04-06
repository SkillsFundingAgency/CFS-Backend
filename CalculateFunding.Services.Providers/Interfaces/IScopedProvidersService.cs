using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IScopedProvidersService
    {
        Task<IActionResult> FetchCoreProviderData(string specificationId);
        Task<IActionResult> PopulateProviderSummariesForSpecification(string specificationId, string correlationId, Reference user, bool setCachedProviders = false);
        Task<IActionResult> GetScopedProviderIds(string specificationId);
        Task PopulateScopedProviders(Message message);
    }
}
