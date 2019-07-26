using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Utility;
using Microsoft.AspNetCore.Mvc;
using IPolicyProviderFundingVersionService = CalculateFunding.Services.Providers.Interfaces.IProviderFundingVersionService;

namespace CalculateFunding.Api.External.V3.Services
{
    public class ProviderFundingVersionService: IProviderFundingVersionService
    {
        private readonly IPolicyProviderFundingVersionService _providerFundingVersionService;      

        public ProviderFundingVersionService(IPolicyProviderFundingVersionService providerFundingVersionService)
        {
            Guard.ArgumentNotNull(providerFundingVersionService, nameof(providerFundingVersionService));
            _providerFundingVersionService = providerFundingVersionService;            
        }

        public async Task<IActionResult> GetFunding(string providerFundingVersion)
        {
            IActionResult result = await _providerFundingVersionService.GetProviderFundingVersions(providerFundingVersion);
            return result;
        }

       
    }
}
