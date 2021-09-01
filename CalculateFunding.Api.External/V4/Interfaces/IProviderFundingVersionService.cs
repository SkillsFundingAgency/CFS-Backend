using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Interfaces
{
    public interface IProviderFundingVersionService
    {
        Task<IActionResult> GetProviderFundingVersion(string channel, string providerFundingVersion);
        Task<IActionResult> GetFundings(string channel, string publishedProviderVersion);
    }
}
