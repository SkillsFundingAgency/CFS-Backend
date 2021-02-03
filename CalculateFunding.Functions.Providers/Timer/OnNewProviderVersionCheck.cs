using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Providers.Timer
{
    public class OnNewProviderVersionCheck
    {
        private const string Every1Minute = "*/1 * * * *";
        private readonly IProviderVersionUpdateCheckService _providerVersionUpdateCheckService;
        private readonly IConfigurationRefresher _configurationRefresher;

        public OnNewProviderVersionCheck(
            IProviderVersionUpdateCheckService providerVersionUpdateCheckService,
            IConfigurationRefresherProvider refresherProvider)
        {
            _providerVersionUpdateCheckService = providerVersionUpdateCheckService;

            _configurationRefresher = refresherProvider.Refreshers.First();

        }

        [FunctionName(FunctionConstants.NewProviderVersionCheck)]
        public async Task Run([TimerTrigger(Every1Minute)] TimerInfo timer)
        {
            await _configurationRefresher.TryRefreshAsync();

            await _providerVersionUpdateCheckService.CheckProviderVersionUpdate();
        }
    }
}
