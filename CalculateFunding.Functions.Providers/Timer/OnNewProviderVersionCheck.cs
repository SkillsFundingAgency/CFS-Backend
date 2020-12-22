using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Providers.Timer
{
    public class OnNewProviderVersionCheck
    {
        private const string Every1Minute = "*/1 * * * *";
        private readonly IProviderVersionUpdateCheckService _providerVersionUpdateCheckService;

        public OnNewProviderVersionCheck(IProviderVersionUpdateCheckService providerVersionUpdateCheckService)
        {
            _providerVersionUpdateCheckService = providerVersionUpdateCheckService;
        }

        [FunctionName(FunctionConstants.NewProviderVersionCheck)]
        public async Task Run([TimerTrigger(Every1Minute)] TimerInfo timer)
        {
            await _providerVersionUpdateCheckService.CheckProviderVersionUpdate();
        }
    }
}
