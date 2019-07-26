using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Providers.Interfaces
{
    public interface IProviderFundingVersionService
    {
        Task<IActionResult> GetProviderFundingVersions(string providerFundingVersion);
    }
}
