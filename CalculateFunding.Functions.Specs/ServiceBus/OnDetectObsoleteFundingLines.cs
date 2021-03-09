using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Specs.ServiceBus
{
    public class OnDetectObsoleteFundingLines : SmokeTest
    {
        private readonly IConfigurationRefresher _configurationRefresher;

        public const string FunctionName = "on-detect-obsolete-funding-lines";
        private const string DetectObsoleteFundingLines = ServiceBusConstants.QueueNames.DetectObsoleteFundingLines;

        public OnDetectObsoleteFundingLines(
            ILogger logger,
            IObsoleteFundingLineAndEnumDetection obsoleteFundingLineAndEnumDetection,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider, obsoleteFundingLineAndEnumDetection)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(obsoleteFundingLineAndEnumDetection, nameof(obsoleteFundingLineAndEnumDetection));
            Guard.ArgumentNotNull(refresherProvider, nameof(refresherProvider));

            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(DetectObsoleteFundingLines, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await _configurationRefresher.TryRefreshAsync();

            await base.Run(message);
        }
    }
}
