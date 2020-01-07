using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IScopedProvidersService
    {
        Task<IActionResult> FetchCoreProviderData(string specificationId);
        Task<IActionResult> PopulateProviderSummariesForSpecification(string specificationId);
        Task<IActionResult> GetScopedProviderIds(string specificationId);
    }
}
