using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.AppConfiguration
{
    public class LocalConfigurationRefresher : IConfigurationRefresher
    {
        public Uri AppConfigurationEndpoint => throw new NotImplementedException();

        public Task RefreshAsync()
        {
            throw new NotImplementedException();
        }

        public void SetDirty(TimeSpan? maxDelay = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryRefreshAsync()
        {
            return Task.FromResult(true);
        }
    }
}
