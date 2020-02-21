using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IProviderFundingVersionService
    {
        Task<IActionResult> GetProviderFundingVersion(string providerFundingVersion);
        Task<IActionResult> GetFundings(string publishedProviderVersion);
    }
}
