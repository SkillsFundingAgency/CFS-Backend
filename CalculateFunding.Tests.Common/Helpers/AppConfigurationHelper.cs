using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using NSubstitute;
using System.Collections.Generic;

namespace CalculateFunding.Tests.Common.Helpers
{
    public static class AppConfigurationHelper
    {
        public static IConfigurationRefresherProvider CreateConfigurationRefresherProvider()
        {
            IConfigurationRefresher configurationRefresher = Substitute.For<IConfigurationRefresher>();
            IEnumerable<IConfigurationRefresher> configurationRefreshers = new List<IConfigurationRefresher> { configurationRefresher };

            IConfigurationRefresherProvider configurationRefresherProvider = Substitute.For<IConfigurationRefresherProvider>();

            configurationRefresherProvider.Refreshers.Returns(configurationRefreshers);

            return configurationRefresherProvider;
        }
    }
}
