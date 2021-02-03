using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System.Collections.Generic;

namespace CalculateFunding.Services.Core.AppConfiguration
{
    public class LocalAppConfigurationRefreshProvider : IConfigurationRefresherProvider
    {
        public IEnumerable<IConfigurationRefresher> Refreshers => new List<IConfigurationRefresher> { new LocalConfigurationRefresher() };
    }
}
